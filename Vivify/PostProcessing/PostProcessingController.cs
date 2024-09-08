using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA.Utilities;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Controllers;
using Vivify.TrackGameObject;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.PostProcessing;

[RequireComponent(typeof(Camera))]
internal class PostProcessingController : CullingCameraController
{
    private readonly Dictionary<CullingTextureTracker, string> _activeCullingTextureDatas = new();
    private readonly Dictionary<DeclareRenderTextureData, string> _activeDeclaredTextures = new();
    private readonly Dictionary<string, CullingCameraController> _cullingCameraControllers = new();
    private readonly Dictionary<string, RenderTextureHolder> _declaredTextures = new();
    private readonly Stack<CullingTextureController> _disabledCullingCameraControllers = new();

    private readonly List<CullingTextureTracker> _reusableCullingKeys = [];
    private readonly List<DeclareRenderTextureData> _reusableDeclaredKeys = [];

    private int? _defaultCullingMask;

    private SiraLog _log = null!;
    private IInstantiator _instantiator = null!;

    internal static Dictionary<string, CullingTextureTracker> CullingTextureDatas { get; } = new();

    internal static Dictionary<string, DeclareRenderTextureData> DeclaredTextureDatas { get; } = new();

    internal static List<MaterialData> PostProcessingMaterial { get; } = [];

    internal override int DefaultCullingMask => _defaultCullingMask ?? Camera.cullingMask;

    internal static void ResetMaterial()
    {
        foreach (CullingTextureTracker cullingTextureTracker in CullingTextureDatas.Values)
        {
            cullingTextureTracker.Dispose();
        }

        CullingTextureDatas.Clear();
        DeclaredTextureDatas.Clear();

        PostProcessingMaterial.Clear();
    }

    protected override void Awake()
    {
        base.Awake();
        Camera.depth *= 10;
    }

    protected override void OnPreCull()
    {
        foreach ((CullingTextureTracker textureData, string textureName) in _activeCullingTextureDatas)
        {
            if (CullingTextureDatas.ContainsValue(textureData))
            {
                continue;
            }

            CullingCameraController cameraController = _cullingCameraControllers[textureName];
            if (cameraController is CullingTextureController cullingTextureController2)
            {
                cullingTextureController2.gameObject.SetActive(false);
                _disabledCullingCameraControllers.Push(cullingTextureController2);
            }
            else
            {
                CullingTextureData = null;
                _defaultCullingMask = null;
            }

            _cullingCameraControllers.Remove(textureName);
            _reusableCullingKeys.Add(textureData);
        }

        foreach (CullingTextureTracker cullingTextureTracker in _reusableCullingKeys)
        {
            _activeCullingTextureDatas.Remove(cullingTextureTracker);
        }

        _reusableCullingKeys.Clear();

        foreach ((string textureName, CullingTextureTracker textureData) in CullingTextureDatas)
        {
            if (_activeCullingTextureDatas.ContainsKey(textureData))
            {
                continue;
            }

            _activeCullingTextureDatas[textureData] = textureName;

            if (textureName == CAMERA_TARGET)
            {
                _defaultCullingMask ??= Camera.cullingMask;

                CullingTextureData = CullingTextureDatas[textureName];
                _cullingCameraControllers[textureName] = this;
                continue;
            }

            CullingTextureController finalController;
            if (_disabledCullingCameraControllers.Count > 0)
            {
                finalController = _disabledCullingCameraControllers.Pop();
                finalController.gameObject.SetActive(true);
            }
            else
            {
                GameObject newObject = new("CullingCamera");
                newObject.transform.SetParent(transform, false);
                newObject.AddComponent<Camera>();
                finalController = _instantiator.InstantiateComponent<CullingTextureController>(newObject, [this]);
                CopyComponent<BloomPrePass, LateBloomPrePass>(gameObject.GetComponent<BloomPrePass>(), newObject);
            }

            finalController.Init(textureName, CullingTextureDatas[textureName]);
            _cullingCameraControllers[textureName] = finalController;
        }

        foreach (CullingCameraController controller in _cullingCameraControllers.Values)
        {
            if (controller is not CullingTextureController cullingTextureController)
            {
                continue;
            }

            if (cullingTextureController.RenderTextures.TryGetValue(
                    Camera.stereoActiveEye,
                    out RenderTexture colorTexture))
            {
                Shader.SetGlobalTexture(cullingTextureController.Key, colorTexture);
            }

            if (cullingTextureController.RenderTexturesDepth.TryGetValue(
                    Camera.stereoActiveEye,
                    out RenderTexture depthTexture))
            {
                Shader.SetGlobalTexture(cullingTextureController.DepthKey, depthTexture);
            }
        }

        base.OnPreCull();

        // delete old declared textures
        foreach ((DeclareRenderTextureData value, string textureName) in _activeDeclaredTextures)
        {
            if (DeclaredTextureDatas.ContainsValue(value))
            {
                continue;
            }

            RenderTexture? renderTexture = _declaredTextures[textureName].Texture;
            if (renderTexture != null)
            {
                renderTexture.Release();
            }

            _declaredTextures.Remove(textureName);
            _reusableDeclaredKeys.Add(value);
        }

        foreach (DeclareRenderTextureData declareRenderTextureData in _reusableDeclaredKeys)
        {
            _activeDeclaredTextures.Remove(declareRenderTextureData);
        }

        _reusableDeclaredKeys.Clear();

        // instantiate RenderTextureHolders
        foreach ((string textureName, DeclareRenderTextureData declareRenderTextureData) in DeclaredTextureDatas)
        {
            if (_activeDeclaredTextures.ContainsKey(declareRenderTextureData))
            {
                continue;
            }

            _declaredTextures.Add(textureName, new RenderTextureHolder(declareRenderTextureData));
            _activeDeclaredTextures.Add(declareRenderTextureData, textureName);
        }

        // set up declared textures
        foreach (RenderTextureHolder value in _declaredTextures.Values)
        {
            DeclareRenderTextureData data = value.Data;
            RenderTexture? texture = value.Texture;
            if (texture != null)
            {
                Shader.SetGlobalTexture(data.PropertyId, texture);
            }
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

    [UsedImplicitly]
    [Inject]
    private void Construct(SiraLog log, IInstantiator instantiator)
    {
        _log = log;
        _instantiator = instantiator;
    }

    private void OnDestroy()
    {
        foreach (RenderTextureHolder declaredTexturesValue in _declaredTextures.Values)
        {
            RenderTexture? texture = declaredTexturesValue.Texture;
            if (texture != null)
            {
                texture.Release();
            }
        }

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

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        // set up declared textures
        foreach ((string textureName, RenderTextureHolder value) in _declaredTextures)
        {
            DeclareRenderTextureData data = value.Data;
            RenderTexture? texture = value.Texture;
            if (texture != null)
            {
                continue;
            }

            RenderTextureDescriptor descripter = src.descriptor;
            descripter.width = (int)((data.Width ?? descripter.width) / data.XRatio);
            descripter.height = (int)((data.Height ?? descripter.height) / data.YRatio);

            if (data.Format.HasValue)
            {
                RenderTextureFormat format = data.Format.Value;
                descripter.colorFormat = format;
            }

            texture = new RenderTexture(descripter);
            if (data.FilterMode.HasValue)
            {
                texture.filterMode = data.FilterMode.Value;
            }

            value.Texture = texture;
            _log.Debug(
                $"Created: {textureName}, {texture.width} : {texture.height} : {texture.filterMode} : {texture.format}");
        }

        if (PostProcessingMaterial.Count == 0)
        {
            Graphics.Blit(src, dst);
            return;
        }

        // blit all passes
        RenderTexture main = RenderTexture.GetTemporary(src.descriptor);
        Graphics.Blit(src, main);
        for (int i = PostProcessingMaterial.Count - 1; i >= 0; i--)
        {
            MaterialData materialData = PostProcessingMaterial[i];
            if (materialData.Frame != null && materialData.Frame != Time.frameCount)
            {
                PostProcessingMaterial.RemoveAt(i);
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

                        RenderTexture temp = RenderTexture.GetTemporary(main.descriptor);
                        Graphics.Blit(main, temp, material, materialData.Pass);
                        RenderTexture.ReleaseTemporary(main);
                        main = temp;
                    }
                    else
                    {
                        if (_declaredTextures.TryGetValue(materialDataTarget, out RenderTextureHolder value))
                        {
                            Blit(main, value.Texture, material, materialData.Pass);
                        }
                        else
                        {
                            _log.Warn($"Unable to find destination [{materialDataTarget}]");
                        }
                    }
                }
            }
            else if (_declaredTextures.TryGetValue(materialData.Source, out RenderTextureHolder sourceHolder))
            {
                foreach (string materialDataTarget in materialData.Targets)
                {
                    RenderTexture? source = sourceHolder.Texture;
                    if (materialDataTarget == CAMERA_TARGET)
                    {
                        Blit(source, main, material, materialData.Pass);
                    }
                    else
                    {
                        if (_declaredTextures.TryGetValue(materialDataTarget, out RenderTextureHolder targetHolder))
                        {
                            // extra stuff becuase we cannot blit directly into itself
                            if (sourceHolder == targetHolder)
                            {
                                if (material == null)
                                {
                                    return;
                                }

                                RenderTexture temp = RenderTexture.GetTemporary(source!.descriptor);
                                temp.filterMode = source.filterMode;
                                Graphics.Blit(source, temp, material, materialData.Pass);
                                Graphics.Blit(temp, targetHolder.Texture);
                                RenderTexture.ReleaseTemporary(temp);
                            }
                            else
                            {
                                Blit(source, targetHolder.Texture, material, materialData.Pass);
                            }
                        }
                        else
                        {
                            _log.Warn($"Unable to find destination [{materialDataTarget}]");
                        }
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
                    cullingTextureController.RenderTextures.TryGetValue(
                        Camera.stereoActiveEye,
                        out RenderTexture? source);
                    if (materialDataTarget == CAMERA_TARGET)
                    {
                        Blit(source, main, material, materialData.Pass);
                    }
                    else
                    {
                        if (_declaredTextures.TryGetValue(materialDataTarget, out RenderTextureHolder targetHolder))
                        {
                            Blit(source, targetHolder.Texture, material, materialData.Pass);
                        }
                        else
                        {
                            _log.Warn($"Unable to find destination [{materialDataTarget}]");
                        }
                    }
                }
            }
            else
            {
                _log.Warn($"Unable to find source [{materialData.Source}]");
            }

            continue;

            static void Blit(RenderTexture? blitsrc, RenderTexture? blitdst, Material? blitmat, int blitpass)
            {
                if (blitdst == null || blitsrc == null)
                {
                    return;
                }

                if (blitmat != null)
                {
                    Graphics.Blit(blitsrc, blitdst, blitmat, blitpass);
                }
                else
                {
                    Graphics.Blit(blitsrc, blitdst);
                }
            }
        }

        Graphics.Blit(main, dst);
        RenderTexture.ReleaseTemporary(main);
    }
}

internal readonly struct MaterialData : IComparable<MaterialData>
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
