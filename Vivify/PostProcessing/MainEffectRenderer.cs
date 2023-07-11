using UnityEngine;

namespace Vivify.PostProcessing
{
    // SetTargetBuffers for some reason causes OnRenderImage to recieve a null source.
    // This class allows anyone to apply effects to any render texture
    internal class MainEffectRenderer
    {
        private readonly MainEffectController _mainEffectController;
        private readonly MainEffectContainerSO _mainEffectContainer;

        internal MainEffectRenderer(MainEffectController mainEffectController)
        {
            _mainEffectController = mainEffectController;
            _mainEffectContainer = _mainEffectController._mainEffectContainer;
        }

        internal void Render(RenderTexture src, RenderTexture dest)
        {
            if (_mainEffectContainer.mainEffect.hasPostProcessEffect)
            {
                _mainEffectController.OnPreRender();
                _mainEffectController.ImageEffectControllerCallback(src, dest);
                _mainEffectController.OnPostRender();
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }
}
