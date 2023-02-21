using CustomJSONData.CustomBeatmap;
using Vivify.Controllers.TrackGameObject;
using Vivify.PostProcessing;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void DestroyTexture(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out DestroyTextureData? heckData))
            {
                return;
            }

            string[] names = heckData.Name;
            foreach (string name in names)
            {
                if (PostProcessingController.CullingMasks.TryGetValue(name, out CullingMaskController? active))
                {
                    PostProcessingController.CullingMasks.Remove(name);
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
