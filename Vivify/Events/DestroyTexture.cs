using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Deserialize;
using Heck.Event;
using SiraUtil.Logging;
using Vivify.HarmonyPatches;
using Vivify.TrackGameObject;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events;

[CustomEvent(DESTROY_TEXTURE)]
internal class DestroyTexture : ICustomEvent
{
    private readonly SiraLog _log;
    private readonly DeserializedData _deserializedData;
    private readonly PostProcessingEffectApplier _postProcessingEffectApplier;

    private DestroyTexture(
        SiraLog log,
        [Inject(Id = ID)] DeserializedData deserializedData,
        PostProcessingEffectApplier postProcessingEffectApplier)
    {
        _log = log;
        _deserializedData = deserializedData;
        _postProcessingEffectApplier = postProcessingEffectApplier;
    }

    public void Callback(CustomEventData customEventData)
    {
        if (!_deserializedData.Resolve(customEventData, out DestroyTextureData? data))
        {
            return;
        }

        string[] names = data.Name;
        foreach (string name in names)
        {
            if (_postProcessingEffectApplier.CullingTextureDatas.TryGetValue(name, out CullingTextureTracker? active))
            {
                _postProcessingEffectApplier.CullingTextureDatas.Remove(name);
                active.Dispose();
                _log.Debug($"Destroyed culling texture [{name}]");
            }
            else if (_postProcessingEffectApplier.DeclaredTextureDatas.Remove(name))
            {
                _log.Debug($"Destroyed render texture [{name}]");
            }
            else
            {
                _log.Error($"Could not find [{name}]");
            }
        }
    }
}
