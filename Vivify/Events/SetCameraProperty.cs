using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Deserialize;
using Heck.Event;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events;

[CustomEvent(SET_CAMERA_PROPERTY)]
internal class SetCameraProperty : ICustomEvent
{
    private readonly CameraPropertyManager _cameraPropertyManager;
    private readonly DeserializedData _deserializedData;

    private SetCameraProperty(
        CameraPropertyManager cameraPropertyManager,
        [Inject(Id = ID)] DeserializedData deserializedData)
    {
        _cameraPropertyManager = cameraPropertyManager;
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
            _cameraPropertyManager.DepthTextureMode = data.DepthTextureMode.Value;
        }

        if (data.ClearFlags.HasValue)
        {
            _cameraPropertyManager.ClearFlags = data.ClearFlags.Value;
        }

        if (data.BackgroundColor.HasValue)
        {
            _cameraPropertyManager.BackgroundColor = data.BackgroundColor.Value;
        }
    }
}
