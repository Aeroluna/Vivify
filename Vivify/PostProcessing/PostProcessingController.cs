using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using IPA.Utilities;
using UnityEngine;
using Vivify.PostProcessing.TrackGameObject;
using static Vivify.VivifyController;

namespace Vivify.PostProcessing
{
    internal class PostProcessingController : MonoBehaviour
    {
        private static readonly int _mirrorTexPropertyID = Shader.PropertyToID("_ReflectionTex");
        private static readonly FieldAccessor<Mirror, MeshRenderer>.Accessor _mirrorMeshRenderer = FieldAccessor<Mirror, MeshRenderer>.GetAccessor("_renderer");

        private readonly HashSet<RenderTexture> _cullingTextures = new(1);
        private readonly Dictionary<string, RenderTextureHolder> _declaredTextures = new();
        private Camera _camera = null!;
        private GameObject _cullingObject = null!;
        private Camera _cullingCamera = null!;
        private RenderTexture _cullingCameraTexture = null!;

        internal static Dictionary<string, MaskController> Masks { get; private set; } = new();

        internal static Dictionary<string, CullingMaskController> CullingMasks { get; private set; } = new();

        internal static HashSet<DeclareRenderTextureData> DeclaredTextureDatas { get; private set; } = new();

        internal static HashSet<MaterialData> PostProcessingMaterial { get; private set; } = new();

        internal static void ResetMaterial()
        {
            Masks = new Dictionary<string, MaskController>();
            CullingMasks = new Dictionary<string, CullingMaskController>();
            DeclaredTextureDatas = new HashSet<DeclareRenderTextureData>();

            PostProcessingMaterial = new HashSet<MaterialData>();
        }

        private static void CopyComponent<T>(T original, GameObject destination)
            where T : Component
        {
            Type type = original.GetType();
            Component copy = destination.AddComponent(type);
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
            // Set mask texturesd
            /*foreach ((string key, MaskController controller) in Masks)
            {
                int id = Shader.PropertyToID("_TempMask" + key);
                _commandBuffer.GetTemporaryRT(id, -1, -1, 24, FilterMode.Point, RenderTextureFormat.Depth);
                _commandBuffer.SetRenderTarget(id);
                _commandBuffer.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

                foreach (MaskRenderer maskRenderer in controller.MaskRenderers)
                {
                    if (maskRenderer == null || !maskRenderer.isActiveAndEnabled)
                    {
                        continue;
                    }

                    foreach (Renderer renderer in maskRenderer.ChildRenderers)
                    {
                        if (renderer.enabled)
                        {
                            _commandBuffer.DrawRenderer(renderer, renderer.material);
                        }
                    }
                }

                _commandBuffer.SetGlobalTexture(key, id);
            }*/

            // cache mirrors
            // the textures for mirrors has already been made, so we cache the mirror textures,
            // do our rendering on the second camera (which will change the textures of the mirror), than swap our original textures back on
            Material[] mirrorMaterials = MirrorsController.EnabledMirrors.Select(n => _mirrorMeshRenderer(ref n).sharedMaterial).ToArray();
            Texture[] cachedTexture = mirrorMaterials.Select(n => n.GetTexture(_mirrorTexPropertyID)).ToArray();

            foreach ((string key, CullingMaskController controller) in CullingMasks)
            {
                // Set renderers to culling layer
                GameObject[] gameObjects = controller.GameObjects;
                int[] cachedLayers = gameObjects.Select(n => n.layer).ToArray();
                foreach (GameObject renderer in gameObjects)
                {
                    renderer.layer = CULLINGLAYER;
                }

                if (controller.Whitelist)
                {
                    int cachedMask = _cullingCamera.cullingMask;
                    _cullingCamera.cullingMask = 1 << CULLINGLAYER;
                    _cullingCamera.Render();
                    _cullingCamera.cullingMask = cachedMask;
                }
                else
                {
                    _cullingCamera.Render();
                }

                RenderTexture renderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
                Graphics.Blit(_cullingCameraTexture, renderTexture);
                Shader.SetGlobalTexture(key, renderTexture);
                _cullingTextures.Add(renderTexture);

                // DOES NOT WORK WILL FIX LATER
                /*if (controller.DepthTexture)
                {
                    RenderTexture depthTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.Depth);
                    Graphics.Blit(_cullingCameraTexture, depthTexture);
                    _commandBuffer.SetGlobalTexture(pair.Key + "_Depth", depthTexture);
                    _cullingTextures.Add(depthTexture);
                }*/

                // reset renderer layers
                for (int i = 0; i < cachedLayers.Length; i++)
                {
                    gameObjects[i].layer = cachedLayers[i];
                }
            }

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
            foreach ((string _, RenderTextureHolder value) in _declaredTextures)
            {
                DeclareRenderTextureData data = value.Data;

                // TODO: clean better
                RenderTexture? texture = value.Texture;
                if (texture == null)
                {
                    texture = RenderTexture.GetTemporary((int)(Screen.width / data.XRatio), (int)(Screen.height / data.YRatio), 24);
                    value.Texture = texture;
                }

                Shader.SetGlobalTexture(data.PropertyId, texture);
            }

            // blit all passes
            bool cameraTargeted = false;
            IEnumerable<MaterialData> sortedDatas = PostProcessingMaterial.OrderBy(n => n.Priority).Reverse();
            foreach (MaterialData materialData in sortedDatas)
            {
                Material? material = materialData.Material;

                foreach (string materialDataTarget in materialData.Targets)
                {
                    if (materialDataTarget == CAMERA_TARGET)
                    {
                        if (material == null)
                        {
                            continue;
                        }

                        Graphics.Blit(src, dst, material, materialData.Pass);
                        cameraTargeted = true;
                    }
                    else
                    {
                        if (_declaredTextures.TryGetValue(materialDataTarget, out RenderTextureHolder value))
                        {
                            if (material != null)
                            {
                                Graphics.Blit(src, value.Texture, material, materialData.Pass);
                            }
                            else
                            {
                                Graphics.Blit(src, value.Texture);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unable to find target [{materialDataTarget}].");
                        }
                    }
                }
            }

            if (!cameraTargeted)
            {
                Graphics.Blit(src, dst);
            }

            // Release Culling masks
            _cullingTextures.Do(RenderTexture.ReleaseTemporary);
            _cullingTextures.Clear();
        }

        private void Update()
        {
            _cullingCamera.CopyFrom(_camera);
            _cullingCamera.targetTexture = _cullingCameraTexture;
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
            _cullingCameraTexture = new RenderTexture(Screen.width, Screen.height, 24);
            _cullingCamera.targetTexture = _cullingCameraTexture;
            ////CopyComponent(gameObject.GetComponent<BloomPrePass>(), _cullingObject);
            _cullingObject.SetActive(true);

            CopyComponent(gameObject.GetComponent<MainEffectController>(), _cullingObject);

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

            if (_cullingCameraTexture != null)
            {
                _cullingCameraTexture.Release();
            }

            _cullingTextures.Do(n =>
            {
                if (n != null)
                {
                    n.Release();
                }
            });
        }
    }

    internal class MaterialData
    {
        internal MaterialData(Material? material, int priority, string[]? targets, int? pass)
        {
            Material = material;
            Priority = priority;
            Targets = targets ?? new[] { "_Camera" };
            Pass = pass ?? -1;
        }

        internal Material? Material { get; }

        internal int Priority { get; }

        internal string[] Targets { get; }

        internal int Pass { get; }
    }
}
