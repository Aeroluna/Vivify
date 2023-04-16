using CustomJSONData.CustomBeatmap;
using Vivify.Controllers.TrackGameObject;
using Vivify.PostProcessing;

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
            CullingMaskController maskController = new(data.Tracks, data.Whitelist, data.DepthTexture);
            _disposables.Add(maskController);
            PostProcessingController.CullingMasks.Add(name, maskController);
            Log.Logger.Log($"Created culling mask [{name}].");
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
