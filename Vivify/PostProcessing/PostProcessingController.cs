namespace Vivify.PostProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using Heck.Animation;
    using UnityEngine;
    using UnityEngine.Rendering;

    internal enum TextureRequest
    {
        Pass0,
        Pass1,
        Pass2,
        Pass3,
        Pass0_Previous = -1,
        Pass1_Previous = -2,
        Pass2_Previous = -3,
        Pass3_Previous = -4,
        TempCulling = 5
    }

    internal class PostProcessingController : MonoBehaviour
    {
        internal const int TEXTURECOUNT = 4;

        private RenderTexture[]? _previousFrames;
        private bool _doMainRender;
        private CommandBuffer? _commandBuffer;
        private Camera? _camera;
        private GameObject? _cullingObject;
        private Camera? _cullingCamera;
        private RenderTexture? _cullingTexture;

        internal static Dictionary<string, MaskController> Masks { get; private set; } = new Dictionary<string, MaskController>();

        internal static Dictionary<string, MaskController> CullingMasks { get; private set; } = new Dictionary<string, MaskController>();

        internal static MaterialData?[] PostProcessingMaterial { get; private set; } = new MaterialData[TEXTURECOUNT];

        internal static RenderTexture[]? MainRenderTextures { get; private set; }

        internal static void ResetMaterial()
        {
            Masks = new Dictionary<string, MaskController>();
            CullingMasks = new Dictionary<string, MaskController>();
            PostProcessingMaterial = new MaterialData[TEXTURECOUNT];
        }

        private void OnPreRender()
        {
            if (_commandBuffer == null)
            {
                throw new InvalidOperationException("Command buffer was null.");
            }

            _commandBuffer.Clear();

            // Set mask texturesd
            foreach (KeyValuePair<string, MaskController> pair in Masks)
            {
                MaskController controller = pair.Value;
                int id = Shader.PropertyToID("_Temp" + pair.Key);
                _commandBuffer.GetTemporaryRT(id, -1, -1, 24, FilterMode.Bilinear, RenderTextureFormat.Depth);
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

                _commandBuffer.SetGlobalTexture(pair.Key, id);
            }
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (VivifyController.VivifyActive)
            {
                // init previousframes and mainrenders
                if (_previousFrames == null)
                {
                    _previousFrames = new RenderTexture[TEXTURECOUNT];
                    for (int i = 0; i < TEXTURECOUNT; i++)
                    {
                        _previousFrames[i] = new RenderTexture(src.descriptor);
                    }

                    if (gameObject.name == "MainCamera")
                    {
                        _doMainRender = true;

                        if (MainRenderTextures == null)
                        {
                            MainRenderTextures = new RenderTexture[TEXTURECOUNT];
                            for (int i = 0; i < TEXTURECOUNT; i++)
                            {
                                MainRenderTextures[i] = new RenderTexture(src.descriptor);
                            }
                        }
                    }
                }

                RenderTexture texture = RenderTexture.GetTemporary(src.descriptor);
                foreach (KeyValuePair<string, MaskController> pair in CullingMasks)
                {
                    MaskController controller = pair.Value;
                    Renderer[] maskRenderers = controller.MaskRenderers.SelectMany(n => n.ChildRenderers).ToArray();
                    int[] cachedLayers = maskRenderers.Select(n => n.gameObject.layer).ToArray();
                    foreach (Renderer renderer in maskRenderers)
                    {
                        renderer.gameObject.layer = Plugin.CULLINGLAYER;
                    }

                    _cullingCamera!.Render();
                    Graphics.Blit(_cullingTexture, texture);

                    for (int i = 0; i < cachedLayers.Length; i++)
                    {
                        maskRenderers[i].gameObject.layer = cachedLayers[i];
                    }
                }

                // blit all passes
                RenderTexture[] tempTextures = new RenderTexture[TEXTURECOUNT];
                int last = -1;
                for (int i = 0; i < TEXTURECOUNT; i++)
                {
                    MaterialData? materialData = PostProcessingMaterial[i];
                    if (materialData != null)
                    {
                        Material material = materialData.Material;

                        // does not work; only samples from vr cam for some reason
                        /*for (int j = 0; j < TEXTURECOUNT; j++)
                        {
                            Shader.SetGlobalTexture($"_Pass{j}", tempTextures[j]);
                            Shader.SetGlobalTexture($"_Pass{j}_Previous", _previousFrames[j]);
                        }*/

                        foreach (KeyValuePair<string, TextureRequest> pair in materialData.TextureRequests)
                        {
                            int requestId = (int)pair.Value;
                            Texture tex;
                            if (pair.Value == TextureRequest.TempCulling)
                            {
                                tex = texture;
                            }
                            else
                            {
                                if (requestId >= 0)
                                {
                                    tex = tempTextures[requestId];
                                }
                                else
                                {
                                    tex = _previousFrames[Math.Abs(requestId) - 1];
                                }
                            }

                            material.SetTexture(pair.Key, tex);
                        }

                        tempTextures[i] = RenderTexture.GetTemporary(src.descriptor);
                        Graphics.Blit(src, tempTextures[i], material);
                        last = i;
                    }
                }

                // write last pass to screen
                if (last != -1)
                {
                    Graphics.Blit(tempTextures[last], dest);
                }
                else
                {
                    Graphics.Blit(src, dest);
                }

                // finalize/blit to previousframe
                for (int i = 0; i < TEXTURECOUNT; i++)
                {
                    ////RenderTexture.ReleaseTemporary(_previousFrames[i]);
                    if (tempTextures[i] != null)
                    {
                        ////previousFrames[i] = RenderTexture.GetTemporary(src.width, src.height, src.depth, src.format);
                        Graphics.Blit(tempTextures[i], _previousFrames[i]);

                        if (_doMainRender)
                        {
                            if (MainRenderTextures != null)
                            {
                                Graphics.Blit(tempTextures[i], MainRenderTextures[i]);
                            }
                        }
                    }

                    RenderTexture.ReleaseTemporary(tempTextures[i]);
                }

                RenderTexture.ReleaseTemporary(texture);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            _camera.depthTextureMode = DepthTextureMode.Depth;
            _commandBuffer = new CommandBuffer() { name = "PostProcessingBuffer" };
            _camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);

            _cullingObject = new GameObject("CullingCamera");
            _cullingObject.SetActive(false);
            _cullingObject.transform.SetParent(transform, false);
            _cullingCamera = _cullingObject.AddComponent<Camera>();
            _cullingCamera.CopyFrom(_camera);
            _cullingCamera.enabled = false;
            _cullingTexture = new RenderTexture(Screen.width, Screen.height, 24);
            _cullingCamera.targetTexture = _cullingTexture;
            CopyComponent(gameObject.GetComponent<BloomPrePass>(), _cullingObject);
            _cullingObject.SetActive(true);

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

        private T? CopyComponent<T>(T original, GameObject destination)
            where T : Component
        {
            Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }

            return copy as T;
        }

        private void OnDisable()
        {
            if (_cullingObject != null)
            {
                Destroy(_cullingObject);
            }

            if (_commandBuffer != null)
            {
                _camera?.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
                _commandBuffer.Dispose();
                _commandBuffer = null;
            }

            if (_previousFrames != null)
            {
                for (int i = 0; i < _previousFrames.Length; i++)
                {
                    _previousFrames[i].Release();
                }
            }

            if (_cullingTexture != null)
            {
                _cullingTexture.Release();
            }
        }
    }
}
