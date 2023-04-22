using CustomJSONData.CustomBeatmap;
using Vivify.PostProcessing;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void DeclareRenderTexture(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out DeclareRenderTextureData? data))
            {
                return;
            }

            PostProcessingController.DeclaredTextureDatas.Add(data.Name, data);
            Log.Logger.Log($"Created texture [{data.Name}].");
        }
    }
}
