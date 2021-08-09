namespace Vivify
{
    using System;
    using System.Collections.Generic;
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
    }

    internal class PostProcessingController : MonoBehaviour
    {
        internal const int TEXTURECOUNT = 4;

        private RenderTexture[]? _previousFrames;

        private bool _doMainRender;

        private CommandBuffer? _commandBuffer;

        private Camera? _camera;

        internal static Dictionary<string, MaskController> Masks { get; private set; } = new Dictionary<string, MaskController>();

        internal static MaterialData?[] PostProcessingMaterial { get; private set; } = new MaterialData[TEXTURECOUNT];

        internal static RenderTexture[]? MainRenderTextures { get; private set; }

        internal static void ResetMaterial()
        {
            Masks = new Dictionary<string, MaskController>();
            PostProcessingMaterial = new MaterialData[TEXTURECOUNT];
        }

        private void OnPreRender()
        {
            if (_commandBuffer == null)
            {
                throw new InvalidOperationException("Command buffer was null.");
            }

            _commandBuffer.Clear();

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
                    Plugin.Logger.Log(src.descriptor.colorFormat);
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
                            if (requestId >= 0)
                            {
                                tex = tempTextures[requestId];
                            }
                            else
                            {
                                tex = _previousFrames[Math.Abs(requestId) - 1];
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
        }

        private void OnDisable()
        {
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
        }

        internal class MaskController : IDisposable
        {
            private readonly IEnumerable<Track> _tracks;

            internal MaskController(IEnumerable<Track> tracks)
            {
                _tracks = tracks;
                foreach (Track track in tracks)
                {
                    foreach (GameObject gameObject in track.GameObjects)
                    {
                        CreateMaskRenderer(gameObject);
                    }

                    track.OnGameObjectAdded += OnGameObjectAdded;
                    track.OnGameObjectRemoved += OnGameObjectRemoved;
                }
            }

            internal HashSet<MaskRenderer> MaskRenderers { get; } = new HashSet<MaskRenderer>();

            public void Dispose()
            {
                foreach (Track track in _tracks)
                {
                    if (track != null)
                    {
                        track.OnGameObjectAdded -= OnGameObjectAdded;
                        track.OnGameObjectRemoved -= OnGameObjectRemoved;
                    }
                }
            }

            internal void CreateMaskRenderer(GameObject gameObject)
            {
                MaskRenderer maskRenderer = gameObject.GetComponent<MaskRenderer>();
                maskRenderer ??= gameObject.AddComponent<MaskRenderer>();
                AddMaskRenderer(maskRenderer);
            }

            internal void OnGameObjectAdded(GameObject gameObject)
            {
                CreateMaskRenderer(gameObject);
            }

            internal void OnGameObjectRemoved(GameObject gameObject)
            {
                MaskRenderer maskRenderer = gameObject.GetComponent<MaskRenderer>();
                if (maskRenderer != null) {
                    OnMaskRendererDestroyed(maskRenderer);
                }
            }

            internal void AddMaskRenderer(MaskRenderer maskRenderer)
            {
                MaskRenderers.Add(maskRenderer);
                maskRenderer.OnDestroyed += OnMaskRendererDestroyed;
            }

            private void OnMaskRendererDestroyed(MaskRenderer maskRenderer)
            {
                MaskRenderers.Remove(maskRenderer);
                maskRenderer.OnDestroyed -= OnMaskRendererDestroyed;
            }
        }
    }
}
