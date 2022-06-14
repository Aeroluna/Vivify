using CustomJSONData.CustomBeatmap;
using Vivify.PostProcessing;
using Vivify.PostProcessing.TrackGameObject;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void DeclareMask(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out DeclareMaskData? heckData))
            {
                return;
            }

            string name = heckData.Name;
            MaskController maskController = new(heckData.Tracks);
            PostProcessingController.Masks.Add(name, maskController);
            Log.Logger.Log($"Created mask [{name}].");
        }
    }
}
