namespace Vivify.Events
{
    using System;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;

    internal static class DeclareMaskEvent
    {
        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type == "DeclareMask")
            {
                string name = customEventData.data.Get<string>("_name") ?? throw new InvalidOperationException("Mask name not found.");
                PostProcessingController.Masks.Add(name, new PostProcessingController.MaskController());
            }
        }
    }
}
