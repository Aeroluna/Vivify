using CustomJSONData.CustomBeatmap;
using Vivify.PostProcessing;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void DeclareRenderTexture(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out DeclareRenderTextureData? heckData))
            {
                return;
            }

            PostProcessingController.DeclaredTextureDatas.Add(heckData);
            Log.Logger.Log($"Created texture [{heckData.Name}].");
        }
    }
}
