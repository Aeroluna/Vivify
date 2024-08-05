using UnityEngine;

namespace Vivify.PostProcessing;

internal class RenderTextureHolder
{
    internal RenderTextureHolder(DeclareRenderTextureData data)
    {
        Data = data;
    }

    internal DeclareRenderTextureData Data { get; }

    internal RenderTexture? Texture { get; set; }
}
