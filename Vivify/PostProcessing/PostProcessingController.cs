using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using IPA.Utilities;
using UnityEngine;
using Vivify.Controllers.TrackGameObject;
using static Vivify.VivifyController;
using Logger = IPA.Logging.Logger;

namespace Vivify.PostProcessing
{
    [RequireComponent(typeof(Camera))]
    internal class PostProcessingController : MonoBehaviour
    {
        private static readonly int _mirrorTexPropertyID = Shader.PropertyToID("_ReflectionTex");
        private static readonly FieldAccessor<Mirror, MeshRenderer>.Accessor _mirrorMeshRenderer = FieldAccessor<Mirror, MeshRenderer>.GetAccessor("_renderer");

        private readonly HashSet<RenderTexture> _cullingTextures = new();
        private readonly Dictionary<string, RenderTextureHolder> _declaredTextures = new();
        private Camera _camera = null!;
        private GameObject _cullingObject = null!;
        private Camera _cullingCamera = null!;
        private CameraCullingMaskController _cameraCullingMaskController = null!;

        internal static Dictionary<string, CullingMaskController> CullingMasks { get; private set; } = new();

        internal static HashSet<DeclareRenderTextureData> DeclaredTextureDatas { get; private set; } = new();

        internal static HashSet<MaterialData> PostProcessingMaterial { get; private set; } = new();

        internal static void ResetMaterial()
        {
            CullingMasks = new Dictionary<string, CullingMaskController>();
            DeclaredTextureDatas = new HashSet<DeclareRenderTextureData>();

            PostProcessingMaterial = new HashSet<MaterialData>();
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
            _cullingCamera.CopyFrom(_camera);
            _cullingTextures.UnionWith(_cameraCullingMaskController.RenderCullingMasks(CullingMasks));
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            _cameraCullingMaskController.Descriptor = src.descriptor;

            // cache mirrors
            // the textures for mirrors has already been made, so we cache the mirror textures,
            // do our rendering on the second camera (which will change the textures of the mirror), than swap our original textures back on
            Material[] mirrorMaterials = MirrorsController.EnabledMirrors.Select(n => _mirrorMeshRenderer(ref n).sharedMaterial).ToArray();
            Texture[] cachedTexture = mirrorMaterials.Select(n => n.GetTexture(_mirrorTexPropertyID)).ToArray();

            // clean mirrors
            for (int i = 0; i < mirrorMaterials.Length; i++)
            {
                mirrorMaterials[i].SetTexture(_mirrorTexPropertyID, cachedTexture[i]);
            }

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
                else
                {
                    Log.Logger.Log($"Unable to find source [{materialData.Source}].", Logger.Level.Error);
                }
            }

            Graphics.Blit(main, dst);
            RenderTexture.ReleaseTemporary(main);

            // Release Culling masks
            _cullingTextures.Do(RenderTexture.ReleaseTemporary);
            _cullingTextures.Clear();
        }

        private void Start()
        {
            _camera = GetComponent<Camera>();

            _cullingObject = new GameObject("CullingCamera");
            _cullingObject.SetActive(false);
            _cullingObject.transform.SetParent(transform, false);
            _cullingCamera = _cullingObject.AddComponent<Camera>();
            _cullingCamera.CopyFrom(_camera);
            _cullingCamera.enabled = false;
            _cameraCullingMaskController = _cullingObject.AddComponent<CameraCullingMaskController>();
            ////CopyComponent(gameObject.GetComponent<BloomPrePass>(), _cullingObject);
            _cullingObject.SetActive(true);
            CopyComponent<BloomPrePass, LateBloomPrePass>(gameObject.GetComponent<BloomPrePass>(), _cullingObject);

            MirrorsController.UpdateMirrors();

            /*
            _cullingCamera.cullingMask &= ~(1 << 0);

            string binary = Convert.ToString(_cullingCamera.cullingMask, 2);
            binary = new string(binary.ToCharArray().Reverse().ToArray());
            List<int> layers = new List<int>();
            for (int i = 0; i < binary.Length; i++)
            {
                if (binary[i] == '0')
                {
                    layers.Add(i);
                }
            }
            Plugin.Logger.Log($"{gameObject.name}: {binary}");
            Plugin.Logger.Log($"culling: {string.Join(", ", layers)}");
            layers.ForEach(n => Plugin.Logger.Log($"{n}: {LayerMask.LayerToName(n)}"));*/
        }

        private void OnDestroy()
        {
            if (_cullingObject != null)
            {
                Destroy(_cullingObject);
            }

            _declaredTextures.Values.Do(n => RenderTexture.ReleaseTemporary(n.Texture));
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
