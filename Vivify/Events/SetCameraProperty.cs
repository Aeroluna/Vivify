using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Deserialize;
using Heck.Event;
using Vivify.Managers;
using Vivify.TrackGameObject;
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
        if (!_deserializedData.Resolve(customEventData, out SetCameraPropertyData? eventData))
        {
            return;
        }

        SetCameraProperties(eventData.Id, eventData.Property);
    }

    public void SetCameraProperties(string id, CameraProperty property)
    {
        if (!_cameraPropertyManager.Properties.TryGetValue(
                id,
                out CameraPropertyManager.CameraProperties properties))
        {
            _cameraPropertyManager.Properties[id] = properties = new CameraPropertyManager.CameraProperties();
        }

        if (property.HasDepthTextureMode)
        {
            properties.DepthTextureMode = property.DepthTextureMode;
        }

        if (property.HasClearFlags)
        {
            properties.ClearFlags = property.ClearFlags;
        }

        if (property.HasBackgroundColor)
        {
            properties.BackgroundColor = property.BackgroundColor;
        }

        if (property.HasCulling)
        {
            CameraProperty.CullingData? cullingData = property.Culling;
            properties.CullingTextureData = cullingData != null
                ? new CullingTextureTracker(cullingData.Tracks, cullingData.Whitelist)
                : null;
        }

        if (property.HasBloomPrePass)
        {
            properties.BloomPrePass = property.BloomPrePass;
        }

        if (property.HasMainEffect)
        {
            properties.MainEffect = property.MainEffect;
        }
    }
}
