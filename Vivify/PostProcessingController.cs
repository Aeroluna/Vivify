namespace Vivify
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Vivify.Events;
    using UnityEngine;

    internal class PostProcessingController : MonoBehaviour
    {
        internal static MaterialData[] PostProcessingMaterial { get; private set; } = new MaterialData[4];

        private readonly RenderTexture[] _previousFrames = new RenderTexture[4];

        private enum TextureRequest
        {
            Pass0,
            Pass1,
            Pass2,
            Pass3,
            Pass0_Previous = -1,
            Pass1_Previous = -2,
            Pass2_Previous = -3,
            Pass3_Previous = -4
        }

        internal static void ResetMaterial()
        {
            PostProcessingMaterial = new MaterialData[4];
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (VivifyController.VivifyActive)
            {
                int length = PostProcessingMaterial.Length;
                RenderTexture[] tempTextures = new RenderTexture[length];
                int last = -1;
                for (int i = 0; i < length; i++)
                {
                    if (PostProcessingMaterial[i] != null)
                    {
                        Material material = PostProcessingMaterial[i].Material;

                        foreach (KeyValuePair<string, string> pair in PostProcessingMaterial[i].TextureRequests)
                        {
                            int request = (int)Enum.Parse(typeof(TextureRequest), pair.Value);
                            Texture tex;
                            if (request >= 0)
                            {
                                tex = tempTextures[request];
                            }
                            else
                            {
                                tex = _previousFrames[Math.Abs(request) - 1];
                            }

                            material.SetTexture(pair.Key, tex);
                        }

                        tempTextures[i] = RenderTexture.GetTemporary(src.width, src.height, src.depth, src.format);
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

                for (int i = 0; i < length; i++)
                {
                    //RenderTexture.ReleaseTemporary(_previousFrames[i]);
                    if (tempTextures[i] != null)
                    {
                        //_previousFrames[i] = RenderTexture.GetTemporary(src.width, src.height, src.depth, src.format);
                        Graphics.Blit(tempTextures[i], _previousFrames[i]);
                    }

                    RenderTexture.ReleaseTemporary(tempTextures[i]);
                }
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        private void Awake()
        {
            Camera camera = GetComponent<Camera>();
            for (int i = 0; i < _previousFrames.Length; i++)
            {
                _previousFrames[i] = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _previousFrames.Length; i++)
            {
                _previousFrames[i].Release();
            }
        }
    }
}
