using CustomJSONData.CustomBeatmap;
using Vivify.PostProcessing;
using Vivify.TrackGameObject;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void DestroyTexture(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out DestroyTextureData? data))
            {
                return;
            }

            string[] names = data.Name;
            foreach (string name in names)
            {
                if (PostProcessingController.CullingTextureDatas.TryGetValue(name, out CullingTextureData? active))
                {
                    PostProcessingController.CullingTextureDatas.Remove(name);
                    active.Dispose();
                    _disposables.Remove(active);
                    Log.Logger.Log($"Destroyed [{name}].");
                }
                else
                {
                    Log.Logger.Log($"Could not find [{name}].");
                }
            }
        }
    }
}
