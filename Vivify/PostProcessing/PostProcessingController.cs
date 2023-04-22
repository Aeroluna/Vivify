using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA.Utilities;
using UnityEngine;
using Vivify.Controllers;
using Vivify.TrackGameObject;
using static Vivify.VivifyController;
using Logger = IPA.Logging.Logger;

namespace Vivify.PostProcessing
{
    [RequireComponent(typeof(Camera))]
    internal class PostProcessingController : CullingCameraController
    {
        private readonly Dictionary<string, RenderTextureHolder> _declaredTextures = new();
        private readonly Dictionary<string, CullingCameraController> _cullingCameraControllers = new();
        private readonly Stack<CullingTextureController> _disabledCullingCameraControllers = new();

        private int? _defaultCullingMask;

        internal static Dictionary<string, CullingTextureData> CullingTextureDatas { get; private set; } = new();

        internal static HashSet<DeclareRenderTextureData> DeclaredTextureDatas { get; private set; } = new();

        internal static HashSet<MaterialData> PostProcessingMaterial { get; private set; } = new();

        internal override int DefaultCullingMask => _defaultCullingMask ?? Camera.cullingMask;

        internal static void ResetMaterial()
        {
            CullingTextureDatas = new Dictionary<string, CullingTextureData>();
            DeclaredTextureDatas = new HashSet<DeclareRenderTextureData>();

            PostProcessingMaterial = new HashSet<MaterialData>();
        }

        protected override void Awake()
        {
            base.Awake();
            Camera.depth *= 10;
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

        private void OnPreRender()
        {
            foreach (string cullingMaskKey in _cullingCameraControllers.Keys.Except(CullingTextureDatas.Keys).ToArray())
            {
                if (_cullingCameraControllers[cullingMaskKey] is CullingTextureController cullingTextureController)
                {
                    cullingTextureController.gameObject.SetActive(false);
                    _disabledCullingCameraControllers.Push(cullingTextureController);
                }
                else
                {
                    CullingTextureData = null;
                    _defaultCullingMask = null;
                }

                _cullingCameraControllers.Remove(cullingMaskKey);
            }

            foreach (string cullingMaskKey in CullingTextureDatas.Keys.Except(_cullingCameraControllers.Keys))
            {
                if (cullingMaskKey == CAMERA_TARGET)
                {
                    _defaultCullingMask ??= Camera.cullingMask;

                    CullingTextureData = CullingTextureDatas[cullingMaskKey];
                    _cullingCameraControllers[cullingMaskKey] = this;
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
                    finalController = newObject.AddComponent<CullingTextureController>();
                    finalController.Construct(this);
                    CopyComponent<BloomPrePass, LateBloomPrePass>(gameObject.GetComponent<BloomPrePass>(), newObject);
                }

                finalController.Init(cullingMaskKey, CullingTextureDatas[cullingMaskKey]);
                _cullingCameraControllers[cullingMaskKey] = finalController;
            }
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            // instantiate declared textures
            foreach (DeclareRenderTextureData declareRenderTextureData in DeclaredTextureDatas)
            {
                if (!_declaredTextures.ContainsKey(declareRenderTextureData.Name))
                {
                    _declaredTextures.Add(declareRenderTextureData.Name, new RenderTextureHolder(declareRenderTextureData));
                }
            }

            // set up declared textures
            foreach ((string textureName, RenderTextureHolder value) in _declaredTextures)
            {
                DeclareRenderTextureData data = value.Data;

                // TODO: clean better
                RenderTexture? texture = value.Texture;
                if (texture == null)
                {
                    RenderTextureDescriptor descripter = src.descriptor;
                    descripter.width = (int)((data.Width ?? descripter.width) / data.XRatio);
                    descripter.height = (int)((data.Height ?? descripter.height) / data.YRatio);

                    if (data.Format.HasValue)
                    {
                        RenderTextureFormat format = data.Format.Value;
                        descripter.colorFormat = format;
                    }

                    texture = RenderTexture.GetTemporary(descripter);
                    if (data.FilterMode.HasValue)
                    {
                        texture.filterMode = data.FilterMode.Value;
                    }

                    value.Texture = texture;
                    Log.Logger.Log($"Created: {textureName}, {texture.width} : {texture.height} : {texture.filterMode} : {texture.format}.");
                }

                Shader.SetGlobalTexture(data.PropertyId, texture);
            }

            // blit all passes
            RenderTexture main = RenderTexture.GetTemporary(src.descriptor);
            Graphics.Blit(src, main);
            IEnumerable<MaterialData> sortedDatas = PostProcessingMaterial.OrderBy(n => n.Priority).Reverse();
            foreach (MaterialData materialData in sortedDatas)
            {
                if (materialData.Frame != null && materialData.Frame != Time.frameCount)
                {
                    PostProcessingMaterial.Remove(materialData);
                    continue;
                }

                Material? material = materialData.Material;

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
                                Log.Logger.Log($"Unable to find destination [{materialDataTarget}].", Logger.Level.Error);
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
                                Log.Logger.Log($"Unable to find destination [{materialDataTarget}].", Logger.Level.Error);
                            }
                        }
                    }
                }
                else if (_cullingCameraControllers.TryGetValue(materialData.Source, out CullingCameraController cullingCameraController)
                        && cullingCameraController is CullingTextureController cullingTextureController)
                {
                    foreach (string materialDataTarget in materialData.Targets)
                    {
                        RenderTexture? source = cullingTextureController.RenderTexture;
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
                                Log.Logger.Log($"Unable to find destination [{materialDataTarget}].", Logger.Level.Error);
                            }
                        }
                    }
                }
                else
                {
                    Log.Logger.Log($"Unable to find source [{materialData.Source}].", Logger.Level.Error);
                }
            }

            Graphics.Blit(main, dst);
            RenderTexture.ReleaseTemporary(main);
        }

        private void OnDestroy()
        {
            foreach (RenderTextureHolder declaredTexturesValue in _declaredTextures.Values)
            {
                RenderTexture.ReleaseTemporary(declaredTexturesValue.Texture);
            }

            foreach (CullingCameraController cullingCameraController in _cullingCameraControllers.Values.Concat(_disabledCullingCameraControllers))
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

    internal class MaterialData
    {
        internal MaterialData(Material? material, int priority, string? source, string[]? targets, int? pass, int? frame = null)
        {
            Material = material;
            Priority = priority;
            Source = source ?? CAMERA_TARGET;
            Targets = targets ?? new[] { CAMERA_TARGET };
            Pass = pass ?? -1;
            Frame = frame;
        }

        internal Material? Material { get; }

        internal int Priority { get; }

        internal string Source { get; }

        internal string[] Targets { get; }

        internal int Pass { get; }

        internal int? Frame { get; }
    }
}
