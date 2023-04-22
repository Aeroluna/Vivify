using CustomJSONData.CustomBeatmap;
using Vivify.Controllers;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void SetCameraProperty(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out SetCameraPropertyData? data))
            {
                return;
            }

            if (data.DepthTextureMode.HasValue)
            {
                CameraPropertyController.DepthTextureMode = data.DepthTextureMode.Value;
            }
        }
    }
}
