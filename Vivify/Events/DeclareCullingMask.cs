using CustomJSONData.CustomBeatmap;
using Vivify.PostProcessing;
using Vivify.TrackGameObject;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void DeclareCullingMask(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out DeclareCullingMaskData? data))
            {
                return;
            }

            string name = data.Name;
            CullingTextureData textureData = new(data.Tracks, data.Whitelist, data.DepthTexture);
            _disposables.Add(textureData);
            PostProcessingController.CullingTextureDatas.Add(name, textureData);
            _log.Debug($"Created culling mask [{name}]");
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
}
