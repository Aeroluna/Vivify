using System.Collections.Generic;
using UnityEngine;

namespace Vivify.PostProcessing;

internal class RenderTextureHolder
{
    internal RenderTextureHolder(CreateScreenTextureData data)
    {
        Data = data;
    }

    internal CreateScreenTextureData Data { get; }

    internal Dictionary<Camera.MonoOrStereoscopicEye, RenderTexture> Textures { get; } = new();
}
