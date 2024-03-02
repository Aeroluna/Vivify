using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Event;
using Vivify.Controllers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events
{
    [CustomEvent(SET_CAMERA_PROPERTY)]
    internal class SetCameraProperty : ICustomEvent
    {
        private readonly DeserializedData _deserializedData;

        private SetCameraProperty(
            [Inject(Id = ID)] DeserializedData deserializedData)
        {
            _deserializedData = deserializedData;
        }

        public void Callback(CustomEventData customEventData)
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
