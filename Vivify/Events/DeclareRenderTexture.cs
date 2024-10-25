using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Deserialize;
using Heck.Event;
using SiraUtil.Logging;
using Vivify.HarmonyPatches;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events;

[CustomEvent(DECLARE_TEXTURE)]
internal class DeclareRenderTexture : ICustomEvent
{
    private readonly SiraLog _log;
    private readonly DeserializedData _deserializedData;
    private readonly CameraEffectApplier _cameraEffectApplier;

    private DeclareRenderTexture(
        SiraLog log,
        [Inject(Id = ID)] DeserializedData deserializedData,
        CameraEffectApplier cameraEffectApplier)
    {
        _log = log;
        _deserializedData = deserializedData;
        _cameraEffectApplier = cameraEffectApplier;
    }

    public void Callback(CustomEventData customEventData)
    {
        if (!_deserializedData.Resolve(customEventData, out CreateScreenTextureData? data))
        {
            return;
        }

        _cameraEffectApplier.DeclaredTextureDatas.Add(data.Name, data);
        _log.Debug($"Created texture [{data.Name}]");
    }
}
