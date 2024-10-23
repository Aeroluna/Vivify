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

[CustomEvent(DECLARE_CULLING_TEXTURE)]
internal class DeclareCullingTexture : ICustomEvent
{
    private readonly SiraLog _log;
    private readonly DeserializedData _deserializedData;
    private readonly PostProcessingEffectApplier _postProcessingEffectApplier;

    private DeclareCullingTexture(
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
        if (!_deserializedData.Resolve(customEventData, out DeclareCullingTextureData? data))
        {
            return;
        }

        string name = data.Name;
        CullingTextureTracker textureTracker = new(data.Tracks, data.Whitelist, data.DepthTexture);
        _postProcessingEffectApplier.CullingTextureDatas.Add(name, textureTracker);
        _log.Debug($"Created culling texture [{name}]");
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
