namespace Vivify.Events
{
    using System;
    using System.Collections.Generic;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using Heck.Animation;
    using Vivify.PostProcessing;

    internal static class DeclareMaskEvent
    {
        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type == "DeclareMask")
            {
                string name = customEventData.data.Get<string>("_name") ?? throw new InvalidOperationException("Mask name not found.");
                IEnumerable<Track> tracks = AnimationHelper.GetTrackArray(customEventData.data, EventController.Instance!.CustomEventCallbackController!.BeatmapData!)
                    ?? throw new InvalidOperationException("No tracks found.");
                MaskController maskController = new MaskController(tracks);
                PostProcessingController.Masks.Add(name, maskController);
                Plugin.Logger.Log($"Created mask [{name}].");
            }
        }
    }
}
