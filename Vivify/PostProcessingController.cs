namespace Vivify
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

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

        internal static MaterialData?[] PostProcessingMaterial { get; private set; } = new MaterialData[TEXTURECOUNT];

        internal static RenderTexture[]? MainRenderTextures { get; private set; }

        internal static void ResetMaterial()
        {
            PostProcessingMaterial = new MaterialData[TEXTURECOUNT];
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

                RenderTexture[] tempTextures = new RenderTexture[TEXTURECOUNT];
                int last = -1;
                for (int i = 0; i < TEXTURECOUNT; i++)
                {
                    MaterialData? materialData = PostProcessingMaterial[i];
                    if (materialData != null)
                    {
                        Material material = materialData.Material;

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

                if (last != -1)
                {
                    Graphics.Blit(tempTextures[last], dest);
                }
                else
                {
                    Graphics.Blit(src, dest);
                }

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

        private void OnDestroy()
        {
            if (_previousFrames != null)
            {
                for (int i = 0; i < _previousFrames.Length; i++)
                {
                    _previousFrames[i].Release();
                }
            }
        }
    }
}
