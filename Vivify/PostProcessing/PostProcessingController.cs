using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using Vivify.PostProcessing.TrackGameObject;
using static Vivify.VivifyController;

namespace Vivify.PostProcessing
{
    internal class PostProcessingController : MonoBehaviour
    {
        internal const int TEXTURECOUNT = 4;

        private static readonly int _mirrorTexPropertyID = Shader.PropertyToID("_ReflectionTex");
        private readonly FieldAccessor<Mirror, MeshRenderer>.Accessor _mirrorMeshRenderer = FieldAccessor<Mirror, MeshRenderer>.GetAccessor("_renderer");

        // TODO: fix nullability mess
        private readonly RenderTexture?[] _previousFrames = new RenderTexture[TEXTURECOUNT];
        private List<RenderTexture> _cullingTextures = new();
        private CommandBuffer? _commandBuffer;
        private Camera? _camera;
        private GameObject? _cullingObject;
        private Camera? _cullingCamera;
        private RenderTexture? _cullingCameraTexture;

        internal static Dictionary<string, MaskController> Masks { get; private set; } = new();

        internal static Dictionary<string, CullingMaskController> CullingMasks { get; private set; } = new();

        internal static MaterialData?[] PostProcessingMaterial { get; private set; } = new MaterialData[TEXTURECOUNT];

        internal static void ResetMaterial()
        {
            Masks = new Dictionary<string, MaskController>();
            CullingMasks = new Dictionary<string, CullingMaskController>();

            PostProcessingMaterial = new MaterialData[TEXTURECOUNT];
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

        private void OnPreRender()
        {
            if (_commandBuffer == null)
            {
                throw new InvalidOperationException("Command buffer was null.");
            }

            _commandBuffer.Clear();

            int mainTex = Shader.PropertyToID("_TempMainTex");
            _commandBuffer.GetTemporaryRT(mainTex, -1, -1, 24, FilterMode.Point, RenderTextureFormat.ARGB32);
            _commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, mainTex);

            // Set mask texturesd
            foreach ((string key, MaskController controller) in Masks)
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
            }

            // Culling masks
            _cullingTextures.ForEach(RenderTexture.ReleaseTemporary);
            _cullingTextures.Clear();

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
                    int cachedMask = _cullingCamera!.cullingMask;
                    _cullingCamera.cullingMask = 1 << CULLINGLAYER;
                    _cullingCamera.Render();
                    _cullingCamera.cullingMask = cachedMask;
                }
                else
                {
                    _cullingCamera!.Render();
                }

                RenderTexture renderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
                Graphics.Blit(_cullingCameraTexture, renderTexture);
                _commandBuffer.SetGlobalTexture(key, renderTexture);
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

            // get previous texes
            for (int i = 0; i < TEXTURECOUNT; i++)
            {
                if (_previousFrames[i] != null)
                {
                    _commandBuffer.SetGlobalTexture($"_Pass{i}_Previous", _previousFrames[i]);
                }
            }

            // blit all passes
            int?[] sucessfulPasses = new int?[TEXTURECOUNT];
            int? last = null;
            for (int i = 0; i < TEXTURECOUNT; i++)
            {
                MaterialData? materialData = PostProcessingMaterial[i];
                if (materialData == null)
                {
                    continue;
                }

                Material material = materialData.Material;

                int id = Shader.PropertyToID("_TempPass" + i);
                _commandBuffer.GetTemporaryRT(id, -1, -1, 24, FilterMode.Point, RenderTextureFormat.ARGB32);
                _commandBuffer.Blit(mainTex, id, material);
                _commandBuffer.SetGlobalTexture($"_Pass{i}", id);
                last = id;

                sucessfulPasses[i] = id;
            }

            // save previous frames
            for (int i = 0; i < TEXTURECOUNT; i++)
            {
                if (sucessfulPasses[i].HasValue)
                {
                    if (_previousFrames[i] == null)
                    {
                        _previousFrames[i] = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
                    }

                    _commandBuffer.Blit(sucessfulPasses[i]!.Value, _previousFrames[i]);
                }
                else
                {
                    RenderTexture.ReleaseTemporary(_previousFrames[i]);
                    _previousFrames[i] = null;
                }
            }

            // blit to camera target
            if (last.HasValue)
            {
                _commandBuffer.Blit(last.Value, BuiltinRenderTextureType.CameraTarget);
            }
        }

        private void Update()
        {
            _cullingCamera!.CopyFrom(_camera);
            _cullingCamera!.targetTexture = _cullingCameraTexture;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.depthTextureMode = DepthTextureMode.Depth;
            _commandBuffer = new CommandBuffer { name = "PostProcessingBuffer" };
            _camera.AddCommandBuffer(CameraEvent.AfterImageEffects, _commandBuffer);

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

            _cullingTextures = new List<RenderTexture>(1);

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

            if (_commandBuffer != null)
            {
                if (_camera != null)
                {
                    _camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
                }

                _commandBuffer.Dispose();
                _commandBuffer = null;
            }

            foreach (RenderTexture? t in _previousFrames)
            {
                RenderTexture.ReleaseTemporary(t);
            }

            if (_cullingCameraTexture != null)
            {
                _cullingCameraTexture.Release();
            }

            _cullingTextures.ForEach(n =>
            {
                if (n != null)
                {
                    n.Release();
                }
            });
        }
    }
}
