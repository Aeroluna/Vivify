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
    private readonly PostProcessingEffectApplier _postProcessingEffectApplier;

    private DeclareRenderTexture(
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
        if (!_deserializedData.Resolve(customEventData, out DeclareRenderTextureData? data))
        {
            return;
        }

        _postProcessingEffectApplier.DeclaredTextureDatas.Add(data.Name, data);
        _log.Debug($"Created texture [{data.Name}]");
    }
}
