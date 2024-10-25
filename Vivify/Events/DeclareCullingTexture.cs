using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Deserialize;
using Heck.Event;
using SiraUtil.Logging;
using Vivify.HarmonyPatches;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events;

[CustomEvent(DECLARE_CULLING_TEXTURE)]
internal class DeclareCullingTexture : ICustomEvent
{
    private readonly SiraLog _log;
    private readonly SetCameraProperty _setCameraProperty;
    private readonly CameraEffectApplier _cameraEffectApplier;
    private readonly DeserializedData _deserializedData;

    private DeclareCullingTexture(
        SiraLog log,
        SetCameraProperty setCameraProperty,
        CameraEffectApplier cameraEffectApplier,
        [Inject(Id = ID)] DeserializedData deserializedData)
    {
        _log = log;
        _setCameraProperty = setCameraProperty;
        _cameraEffectApplier = cameraEffectApplier;
        _deserializedData = deserializedData;
    }

    public void Callback(CustomEventData customEventData)
    {
        if (!_deserializedData.Resolve(customEventData, out CreateCameraData? data))
        {
            return;
        }

        string name = data.Name;
        _cameraEffectApplier.CameraDatas.Add(name, data);
        _log.Debug($"Created camera [{name}]");

        if (data.Property != null)
        {
            _setCameraProperty.SetCameraProperties(name, data.Property);
        }

        /*
            GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            List<int> layers = new List<int>();
            gameObjects.Select(n => n.layer).ToList().ForEach(n =>
            {
                if (!layers.Contains(n))
                {
                    layers.Add(n);
                }
            });
            layers.Sort();
            Plugin.Logger.Log($"used layers: {string.Join(", ", layers)}");*/
    }
}
