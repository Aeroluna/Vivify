using UnityEngine;

namespace Vivify.PostProcessing;

// SetTargetBuffers for some reason causes OnRenderImage to recieve a null source.
// This class allows anyone to apply effects to any render texture
internal class MainEffectRenderer
{
    private readonly MainEffectContainerSO _mainEffectContainer;
    private readonly MainEffectController _mainEffectController;

    internal MainEffectRenderer(MainEffectController mainEffectController)
    {
        _mainEffectController = mainEffectController;
        _mainEffectContainer = _mainEffectController._mainEffectContainer;
    }

    internal void Render(RenderTexture src, RenderTexture dest)
    {
        MainEffectSO mainEffect = _mainEffectContainer.mainEffect;
        if (mainEffect.hasPostProcessEffect)
        {
            _mainEffectController.OnPreRender();
            mainEffect.Render(src, dest, _mainEffectController._fadeValue);
            _mainEffectController.OnPostRender();
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
