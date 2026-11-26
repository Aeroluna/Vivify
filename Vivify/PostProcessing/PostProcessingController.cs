using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA.Utilities;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.PostProcessing;

[RequireComponent(typeof(Camera))]
internal class PostProcessingController : CullingCameraController
{
    private readonly Dictionary<CreateCameraData, string> _activeCreateCameraDatas = new();
    private readonly Dictionary<CreateScreenTextureData, string> _activeDeclaredTextures = new();
    private readonly Dictionary<string, CullingCameraController> _cullingCameraControllers = new();
    private readonly Dictionary<string, RenderTextureHolder> _declaredTextures = new();
    private readonly Stack<SecondaryCameraController> _disabledCullingCameraControllers = new();
    private readonly Stack<ActiveCommandBuffer> _activeCommandBuffers = new();

    private readonly List<CreateCameraData> _reusableCameraKeys = [];
    private readonly List<CreateScreenTextureData> _reusableDeclaredKeys = [];

    private readonly int _mainTex = Shader.PropertyToID("_MainTex");

    private SiraLog _log = null!;
    private IInstantiator _instantiator = null!;

    private ImageEffectController _imageEffectController = null!;
    private RenderTextureDescriptor? _cachedMainDescriptor;

    internal Dictionary<string, CreateCameraData> CameraDatas { get; set; } = new();

    internal Dictionary<string, CreateScreenTextureData> DeclaredTextureDatas { get; set; } = new();

    internal Dictionary<PostProcessingOrder, List<MaterialData>> Effects { get; set; } = new();

    internal void PrewarmCameras(int count)
    {
        count -= _disabledCullingCameraControllers.Count + _cullingCameraControllers.Count;
        for (int i = 0; i < count; i++)
        {
            _disabledCullingCameraControllers.Push(CreateCamera());
        }
    }

    protected override void OnPreCull()
    {
        foreach ((CreateCameraData textureData, string id) in _activeCreateCameraDatas)
        {
            if (CameraDatas.ContainsValue(textureData))
            {
                continue;
            }

            if (_cullingCameraControllers.TryGetValue(id, out CullingCameraController cameraController) &&
                cameraController is SecondaryCameraController secondaryCameraController2)
            {
                secondaryCameraController2.gameObject.SetActive(false);
                _disabledCullingCameraControllers.Push(secondaryCameraController2);
            }

            _cullingCameraControllers.Remove(id);
            _reusableCameraKeys.Add(textureData);
        }

        foreach (CreateCameraData createCameraData in _reusableCameraKeys)
        {
            _activeCreateCameraDatas.Remove(createCameraData);
        }

        _reusableCameraKeys.Clear();

        foreach ((string textureName, CreateCameraData cameraData) in CameraDatas)
        {
            if (_activeCreateCameraDatas.ContainsKey(cameraData))
            {
                continue;
            }

            _activeCreateCameraDatas[cameraData] = textureName;

            SecondaryCameraController finalController = _disabledCullingCameraControllers.Count > 0
                ? _disabledCullingCameraControllers.Pop()
                : CreateCamera();

            finalController.Init(cameraData);
            finalController.gameObject.SetActive(true);
            _cullingCameraControllers[textureName] = finalController;
        }

        // delete old declared textures
        foreach ((CreateScreenTextureData value, string textureName) in _activeDeclaredTextures)
        {
            if (DeclaredTextureDatas.ContainsValue(value))
            {
                continue;
            }

            foreach (RenderTexture? renderTexture in _declaredTextures[textureName].Textures.Values)
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                }
            }

            _declaredTextures.Remove(textureName);
            _reusableDeclaredKeys.Add(value);
        }

        foreach (CreateScreenTextureData declareRenderTextureData in _reusableDeclaredKeys)
        {
            _activeDeclaredTextures.Remove(declareRenderTextureData);
        }

        _reusableDeclaredKeys.Clear();

        // instantiate RenderTextureHolders
        foreach ((string textureName, CreateScreenTextureData declareRenderTextureData) in DeclaredTextureDatas)
        {
            if (_activeDeclaredTextures.ContainsKey(declareRenderTextureData))
            {
                continue;
            }

            _declaredTextures.Add(textureName, new RenderTextureHolder(declareRenderTextureData));
            _activeDeclaredTextures.Add(declareRenderTextureData, textureName);
        }

        base.OnPreCull();
    }

    protected override void OnPostRender()
    {
        ClearActiveCommandBuffers();

        base.OnPostRender();
    }

    // Cool method for copying serialized fields
    // if i was smart, i would've used this for chroma components
    private static void CopyComponent<T, TDerived>(T original, GameObject destination)
        where T : MonoBehaviour
        where TDerived : T
    {
        Type type = typeof(T);
        MonoBehaviour copy = destination.AddComponent<TDerived>();
        FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            if (Attribute.IsDefined(field, typeof(SerializeField)))
            {
                field.SetValue(copy, field.GetValue(original));
            }
        }
    }

    private static void ExecuteAndDispose(CommandBuffer command)
    {
        Graphics.ExecuteCommandBuffer(command);
        command.Dispose();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        RenderTextureDescriptor descriptor = src.descriptor;
        descriptor.msaaSamples = 1;
        _cachedMainDescriptor = descriptor;
        CreateDeclaredTextures(descriptor);

        List<MaterialData> beforeEffects = Effects[PostProcessingOrder.BeforeMainEffect];
        List<MaterialData> afterEffects = Effects[PostProcessingOrder.AfterMainEffect];
        ImageEffectController.RenderImageCallback? callback = _imageEffectController._renderImageCallback;
        bool hasMainEffect = callback != null && _imageEffectController.isActiveAndEnabled;
        bool hasBefore = beforeEffects.Count > 0;
        bool hasAfter = afterEffects.Count > 0;

        // each stage renders straight into dst when nothing follows it,
        // so a frame without any blits costs no extra full screen copies
        RenderTexture current = src;
        RenderTexture? temp = null;

        if (hasBefore)
        {
            bool last = !hasMainEffect && !hasAfter;
            RenderTexture target = last ? dst : RenderTexture.GetTemporary(descriptor);
            ExecuteAndDispose(RenderImage(descriptor, current, target, beforeEffects));
            current = target;
            temp = last ? null : target;
        }

        if (hasMainEffect)
        {
            bool last = !hasAfter;
            RenderTexture target = last ? dst : RenderTexture.GetTemporary(descriptor);
            callback!(current, target);
            if (temp != null)
            {
                RenderTexture.ReleaseTemporary(temp);
            }

            current = target;
            temp = last ? null : target;
        }

        if (hasAfter)
        {
            ExecuteAndDispose(RenderImage(descriptor, current, dst, afterEffects));
        }
        else if (!hasBefore && !hasMainEffect)
        {
            Graphics.Blit(src, dst);
        }

        if (temp != null)
        {
            RenderTexture.ReleaseTemporary(temp);
        }
    }

    private void OnPreRender()
    {
        Camera.MonoOrStereoscopicEye stereoActiveEye = Camera.stereoActiveEye;

        foreach (CullingCameraController controller in _cullingCameraControllers.Values)
        {
            if (controller is not SecondaryCameraController secondaryCameraController)
            {
                continue;
            }

            Camera camera = secondaryCameraController.Camera;
            if (!camera.enabled)
            {
                camera.Render();
            }

            if (secondaryCameraController.Key != null &&
                secondaryCameraController.RenderTextures.TryGetValue(
                    stereoActiveEye,
                    out RenderTexture colorTexture))
            {
                Shader.SetGlobalTexture(secondaryCameraController.Key.Value, colorTexture);
            }

            if (secondaryCameraController.DepthKey != null &&
                secondaryCameraController.RenderTexturesDepth.TryGetValue(
                    stereoActiveEye,
                    out RenderTexture depthTexture))
            {
                Shader.SetGlobalTexture(secondaryCameraController.DepthKey.Value, depthTexture);
            }
        }

        // set declared texture properties
        foreach (RenderTextureHolder value in _declaredTextures.Values)
        {
            CreateScreenTextureData data = value.Data;
            if (value.Textures.TryGetValue(stereoActiveEye, out RenderTexture texture))
            {
                Shader.SetGlobalTexture(data.PropertyId, texture);
            }
        }

        if (!_cachedMainDescriptor.HasValue)
        {
            return;
        }

        CreateDeclaredTextures(_cachedMainDescriptor.Value);

        RenderTargetIdentifier src = new(BuiltinRenderTextureType.CurrentActive);
        RenderTargetIdentifier dst = new(BuiltinRenderTextureType.CameraTarget);

        AddCommandBuffer(PostProcessingOrder.BeforeSkybox, CameraEvent.BeforeSkybox);
        AddCommandBuffer(PostProcessingOrder.AfterSkybox, CameraEvent.AfterSkybox);
        AddCommandBuffer(PostProcessingOrder.BeforeOpaque, CameraEvent.BeforeForwardOpaque);
        AddCommandBuffer(PostProcessingOrder.AfterOpaque, CameraEvent.AfterForwardOpaque);
        AddCommandBuffer(PostProcessingOrder.BeforeAlpha, CameraEvent.BeforeForwardAlpha);
        AddCommandBuffer(PostProcessingOrder.AfterAlpha, CameraEvent.AfterForwardAlpha);
        return;

        void AddCommandBuffer(PostProcessingOrder order, CameraEvent cameraEvent)
        {
            List<MaterialData> materials = Effects[order];

            if (materials.Count == 0)
            {
                return;
            }

            CommandBuffer command = RenderImage(_cachedMainDescriptor.Value, src, dst, materials);
            Camera.AddCommandBuffer(cameraEvent, command);
            _activeCommandBuffers.Push(new ActiveCommandBuffer
            {
                CameraEvent = cameraEvent,
                Command = command
            });
        }
    }

    private void CreateDeclaredTextures(RenderTextureDescriptor descriptor)
    {
        Camera.MonoOrStereoscopicEye stereoActiveEye = Camera.stereoActiveEye;

        // set up declared textures
        foreach ((string textureName, RenderTextureHolder value) in _declaredTextures)
        {
            if (value.Textures.ContainsKey(stereoActiveEye))
            {
                continue;
            }

            CreateScreenTextureData data = value.Data;

            RenderTextureDescriptor newDescriptor = descriptor;
            newDescriptor.width = (int)((data.Width ?? descriptor.width) / data.XRatio);
            newDescriptor.height = (int)((data.Height ?? descriptor.height) / data.YRatio);

            if (data.Format.HasValue)
            {
                RenderTextureFormat format = data.Format.Value;
                newDescriptor.colorFormat = format;
            }

            RenderTexture texture = new(newDescriptor);
            if (data.FilterMode.HasValue)
            {
                texture.filterMode = data.FilterMode.Value;
            }

            value.Textures[stereoActiveEye] = texture;
            _log.Debug(
                $"Created texture for [{gameObject.name}] [{stereoActiveEye}]: {textureName}, {texture.width} : {texture.height} : {texture.filterMode} : {texture.format}");
        }
    }

    private CommandBuffer RenderImage(RenderTextureDescriptor mainDescriptor, RenderTargetIdentifier src, RenderTargetIdentifier dst, List<MaterialData> materials)
    {
        Camera.MonoOrStereoscopicEye stereoActiveEye = Camera.stereoActiveEye;
        CommandBuffer command = new();

        if (materials.Count == 0)
        {
            command.Blit(src, dst);
            return command;
        }

        int tempIDNext = 69;

        // blit all passes
        int mainID = _mainTex; // TODO: I am not actually sure what this should be
        RenderTargetIdentifier main = new(mainID);
        command.GetTemporaryRT(mainID, mainDescriptor);

        command.Blit(src, main);
        for (int i = materials.Count - 1; i >= 0; i--)
        {
            MaterialData materialData = materials[i];
            if (materialData.Frame != null && materialData.Frame != Time.frameCount)
            {
                materials.RemoveAt(i);
                continue;
            }

            Material? material = materialData.Material;

            if (materialData.Source == CAMERA_TARGET)
            {
                foreach (string materialDataTarget in materialData.Targets)
                {
                    if (materialDataTarget == CAMERA_TARGET)
                    {
                        if (material == null)
                        {
                            continue;
                        }

                        int tempID = ++tempIDNext;
                        RenderTargetIdentifier temp = new(tempID);
                        command.GetTemporaryRT(tempID, mainDescriptor);
                        command.Blit(main, temp, material, materialData.Pass);
                        command.ReleaseTemporaryRT(mainID);
                        main = temp;
                        mainID = tempID;
                    }
                    else
                    {
                        if (_declaredTextures.TryGetValue(materialDataTarget, out RenderTextureHolder targetHolder) &&
                            targetHolder.Textures.TryGetValue(stereoActiveEye, out RenderTexture? target))
                        {
                            Blit(main, target, material, materialData.Pass);
                        }
                        else
                        {
                            _log.Warn($"[{gameObject.name}] Unable to find destination [{materialDataTarget}]");
                        }
                    }
                }
            }
            else if (_declaredTextures.TryGetValue(materialData.Source, out RenderTextureHolder sourceHolder))
            {
                foreach (string materialDataTarget in materialData.Targets)
                {
                    sourceHolder.Textures.TryGetValue(stereoActiveEye, out RenderTexture? source);
                    if (materialDataTarget == CAMERA_TARGET)
                    {
                        Blit(source, main, material, materialData.Pass);
                    }
                    else if (_declaredTextures.TryGetValue(materialDataTarget, out RenderTextureHolder targetHolder))
                    {
                        // extra stuff becuase we cannot blit directly into itself
                        if (sourceHolder == targetHolder)
                        {
                            if (material == null)
                            {
                                _log.Warn($"[{materialDataTarget}] Attempting to blit to self without material");
                                continue;
                            }

                            int tempID = ++tempIDNext;
                            RenderTargetIdentifier temp = new(tempID);
                            command.GetTemporaryRT(tempID, source!.descriptor, source.filterMode);
                            command.Blit(source, temp, material, materialData.Pass);
                            command.Blit(temp, source);
                            command.ReleaseTemporaryRT(tempID);

                            continue;
                        }

                        if (targetHolder.Textures.TryGetValue(stereoActiveEye, out RenderTexture? target))
                        {
                            Blit(source, target, material, materialData.Pass);
                        }
                    }
                    else
                    {
                        _log.Warn($"[{gameObject.name}] Unable to find destination [{materialDataTarget}]");
                    }
                }
            }
            else if (_cullingCameraControllers.TryGetValue(
                         materialData.Source,
                         out CullingCameraController cullingCameraController) &&
                     cullingCameraController is SecondaryCameraController secondaryCameraController)
            {
                foreach (string materialDataTarget in materialData.Targets)
                {
                    secondaryCameraController.RenderTextures.TryGetValue(stereoActiveEye, out RenderTexture? source);
                    if (materialDataTarget == CAMERA_TARGET)
                    {
                        Blit(source, main, material, materialData.Pass);
                    }
                    else if (_declaredTextures.TryGetValue(materialDataTarget, out RenderTextureHolder targetHolder) &&
                            targetHolder.Textures.TryGetValue(stereoActiveEye, out RenderTexture? target))
                    {
                        Blit(source, target, material, materialData.Pass);
                    }
                    else
                    {
                        _log.Warn($"[{gameObject.name}] Unable to find destination [{materialDataTarget}]");
                    }
                }
            }
            else
            {
                _log.Warn($"[{gameObject.name}] Unable to find source [{materialData.Source}]");
            }

            continue;

            void Blit(RenderTargetIdentifier? blitSrc, RenderTargetIdentifier? blitDst, Material? blitMat, int blitPass)
            {
                if (!blitDst.HasValue || !blitSrc.HasValue)
                {
                    return;
                }

                if (blitMat != null)
                {
                    command.Blit(blitSrc.Value, blitDst.Value, blitMat, blitPass);
                }
                else
                {
                    command.Blit(blitSrc.Value, blitDst.Value);
                }
            }
        }

        command.Blit(main, dst);
        command.ReleaseTemporaryRT(mainID);
        return command;
    }

    private SecondaryCameraController CreateCamera()
    {
        GameObject newObject = new("VivifyCamera");
        ////newObject.SetActive(false);
        newObject.transform.SetParent(transform, false);
        newObject.AddComponent<Camera>();
        CopyComponent<BloomPrePass, LateBloomPrePass>(gameObject.GetComponent<BloomPrePass>(), newObject);
        SecondaryCameraController result =
            _instantiator.InstantiateComponent<SecondaryCameraController>(newObject, [this]);
        return result;
    }

    private void ClearActiveCommandBuffers()
    {
        while (_activeCommandBuffers.Count > 0)
        {
            ActiveCommandBuffer cmd = _activeCommandBuffers.Pop();
            Camera.RemoveCommandBuffer(cmd.CameraEvent, cmd.Command);
            cmd.Command.Dispose();
        }
    }

    [UsedImplicitly]
    [Inject]
    private void Construct(SiraLog log, IInstantiator instantiator)
    {
        _log = log;
        _instantiator = instantiator;
    }

    private void Awake()
    {
        _imageEffectController = GetComponent<ImageEffectController>();

        foreach (PostProcessingOrder key in Enum.GetValues(typeof(PostProcessingOrder)))
        {
            if (!Effects.ContainsKey(key))
            {
                Effects[key] = [];
            }
        }
    }

    private void OnDestroy()
    {
        foreach (RenderTexture texture in _declaredTextures.Values.SelectMany(n => n.Textures.Values))
        {
            if (texture != null)
            {
                texture.Release();
            }
        }

        _declaredTextures.Clear();

        foreach (CullingCameraController cullingCameraController in _cullingCameraControllers.Values.Concat(
                     _disabledCullingCameraControllers))
        {
            if (cullingCameraController is SecondaryCameraController)
            {
                Destroy(cullingCameraController.gameObject);
            }
        }

        _cullingCameraControllers.Clear();
        _disabledCullingCameraControllers.Clear();
        ClearActiveCommandBuffers();
    }

    private struct ActiveCommandBuffer
    {
        public CommandBuffer Command { get; set; }

        public CameraEvent CameraEvent { get; set; }
    }
}

internal readonly record struct MaterialData : IComparable<MaterialData>
{
    internal MaterialData(
        Material? material,
        int priority,
        string? source,
        string[]? targets,
        int? pass,
        int? frame = null)
    {
        Material = material;
        Priority = priority;
        Source = source ?? CAMERA_TARGET;
        Targets = targets ?? [CAMERA_TARGET];
        Pass = pass ?? -1;
        Frame = frame;
    }

    internal int? Frame { get; }

    internal Material? Material { get; }

    internal int Pass { get; }

    internal int Priority { get; }

    internal string Source { get; }

    internal string[] Targets { get; }

    public int CompareTo(MaterialData other)
    {
        int result = Priority.CompareTo(other.Priority);
        return result < 0 ? -1 : 1;
    }
}
