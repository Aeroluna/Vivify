using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA.Utilities;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
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
    private readonly Stack<CullingTextureController> _disabledCullingCameraControllers = new();

    private readonly List<CreateCameraData> _reusableCameraKeys = [];
    private readonly List<CreateScreenTextureData> _reusableDeclaredKeys = [];

    private int? _defaultCullingMask;

    private SiraLog _log = null!;
    private IInstantiator _instantiator = null!;

    private ImageEffectController _imageEffectController = null!;

    internal Dictionary<string, CreateCameraData> CameraDatas { get; set; } = new();

    internal Dictionary<string, CreateScreenTextureData> DeclaredTextureDatas { get; set; } = new();

    internal List<MaterialData> PreEffects { get; set; } = [];

    internal List<MaterialData> PostEffects { get; set; } = [];

    internal override int DefaultCullingMask => _defaultCullingMask ?? Camera.cullingMask;

    // TODO: make this create the render textures as well
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
        base.OnPreCull();

        foreach ((CreateCameraData textureData, string id) in _activeCreateCameraDatas)
        {
            if (CameraDatas.ContainsValue(textureData))
            {
                continue;
            }

            if (_cullingCameraControllers.TryGetValue(id, out CullingCameraController cameraController) &&
                cameraController is CullingTextureController cullingTextureController2)
            {
                cullingTextureController2.gameObject.SetActive(false);
                _disabledCullingCameraControllers.Push(cullingTextureController2);
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

            CullingTextureController finalController = _disabledCullingCameraControllers.Count > 0
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

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        RenderTextureDescriptor descriptor = src.descriptor;
        CreateDeclaredTextures(descriptor);
        RenderTexture temp = RenderTexture.GetTemporary(descriptor);
        RenderImage(src, temp, PreEffects);

        ImageEffectController.RenderImageCallback? callback = _imageEffectController._renderImageCallback;
        if (callback != null && _imageEffectController.isActiveAndEnabled)
        {
            RenderTexture temp2 = RenderTexture.GetTemporary(descriptor);
            callback(temp, temp2);
            RenderTexture.ReleaseTemporary(temp);
            temp = temp2;
        }

        RenderImage(temp, dst, PostEffects);
        RenderTexture.ReleaseTemporary(temp);
    }

    private void OnPreRender()
    {
        Camera.MonoOrStereoscopicEye stereoActiveEye = Camera.stereoActiveEye;

        foreach (CullingCameraController controller in _cullingCameraControllers.Values)
        {
            if (controller is not CullingTextureController cullingTextureController)
            {
                continue;
            }

            Camera camera = cullingTextureController.Camera;
            if (camera.enabled == false)
            {
                camera.Render();
            }

            if (cullingTextureController.Key != null &&
                cullingTextureController.RenderTextures.TryGetValue(
                    stereoActiveEye,
                    out RenderTexture colorTexture))
            {
                Shader.SetGlobalTexture(cullingTextureController.Key.Value, colorTexture);
            }

            if (cullingTextureController.DepthKey != null &&
                cullingTextureController.RenderTexturesDepth.TryGetValue(
                    stereoActiveEye,
                    out RenderTexture depthTexture))
            {
                Shader.SetGlobalTexture(cullingTextureController.DepthKey.Value, depthTexture);
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

    private void RenderImage(RenderTexture src, RenderTexture dst, List<MaterialData> materials)
    {
        RenderTextureDescriptor descriptor = src.descriptor;
        Camera.MonoOrStereoscopicEye stereoActiveEye = Camera.stereoActiveEye;

        if (materials.Count == 0)
        {
            Graphics.Blit(src, dst);
            return;
        }

        // blit all passes
        RenderTexture main = RenderTexture.GetTemporary(descriptor);
        Graphics.Blit(src, main);
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

                        RenderTexture temp = RenderTexture.GetTemporary(descriptor);
                        Graphics.Blit(main, temp, material, materialData.Pass);
                        RenderTexture.ReleaseTemporary(main);
                        main = temp;
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

                            RenderTexture temp = RenderTexture.GetTemporary(source!.descriptor);
                            temp.filterMode = source.filterMode;
                            Graphics.Blit(source, temp, material, materialData.Pass);
                            Graphics.Blit(temp, source);
                            RenderTexture.ReleaseTemporary(temp);

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
                     cullingCameraController is CullingTextureController cullingTextureController)
            {
                foreach (string materialDataTarget in materialData.Targets)
                {
                    cullingTextureController.RenderTextures.TryGetValue(stereoActiveEye, out RenderTexture? source);
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

            static void Blit(RenderTexture? blitSrc, RenderTexture? blitDst, Material? blitMat, int blitPass)
            {
                if (blitDst == null || blitSrc == null)
                {
                    return;
                }

                if (blitMat != null)
                {
                    Graphics.Blit(blitSrc, blitDst, blitMat, blitPass);
                }
                else
                {
                    Graphics.Blit(blitSrc, blitDst);
                }
            }
        }

        Graphics.Blit(main, dst);
        RenderTexture.ReleaseTemporary(main);
    }

    private CullingTextureController CreateCamera()
    {
        GameObject newObject = new("VivifyCamera");
        newObject.SetActive(false);
        newObject.transform.SetParent(transform, false);
        newObject.AddComponent<Camera>();
        CopyComponent<BloomPrePass, LateBloomPrePass>(gameObject.GetComponent<BloomPrePass>(), newObject);
        CullingTextureController result = _instantiator.InstantiateComponent<CullingTextureController>(newObject, [this]);
        return result;
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
    }

    private void Start()
    {
        _defaultCullingMask = Camera.cullingMask;
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
            if (cullingCameraController is CullingTextureController)
            {
                Destroy(cullingCameraController.gameObject);
            }
        }

        _cullingCameraControllers.Clear();
        _disabledCullingCameraControllers.Clear();
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
