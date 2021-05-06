namespace Vivify
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using UnityEngine;

    internal class PostProcessingController : MonoBehaviour
    {
        internal static Material PostProcessingMaterial { get; set; }

        internal RenderTexture _previousFrame;

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (VivifyController.VivifyActive && PostProcessingMaterial != null)
            {
                bool hasPreviousTex = PostProcessingMaterial.HasProperty("_PreviousTex");
                if (hasPreviousTex)
                {
                    PostProcessingMaterial.SetTexture("_PreviousTex", _previousFrame);
                }

                Graphics.Blit(src, dest, PostProcessingMaterial);

                if (hasPreviousTex)
                {
                    Graphics.Blit(dest, _previousFrame);
                }
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        private void Awake()
        {
            Camera camera = gameObject.GetComponent<Camera>();
            _previousFrame = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 16);
        }

        private void OnDestroy()
        {
            Destroy(_previousFrame);
        }
    }
}
