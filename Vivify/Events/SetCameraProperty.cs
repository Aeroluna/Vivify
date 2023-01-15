using CustomJSONData.CustomBeatmap;
using Vivify.PostProcessing;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void SetCameraProperty(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out SetCameraPropertyData? heckData))
            {
                return;
            }

            if (heckData.DepthTextureMode.HasValue)
            {
                CameraPropertyController.DepthTextureMode = heckData.DepthTextureMode.Value;
            }
        }
    }
}
