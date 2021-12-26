using System;
using System.Collections.Generic;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using Vivify.PostProcessing;
using Vivify.PostProcessing.TrackGameObject;

namespace Vivify.Events
{
    internal static class DeclareMaskEvent
    {
        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type != "DeclareMask")
            {
                return;
            }

            string name = customEventData.data.Get<string>("_name") ?? throw new InvalidOperationException("Mask name not found.");
            IEnumerable<Track> tracks = AnimationHelper.GetTrackArray(customEventData.data, EventController.Instance.CustomEventCallbackController.BeatmapData!)
                                        ?? throw new InvalidOperationException("No tracks found.");
            MaskController maskController = new(tracks);
            PostProcessingController.Masks.Add(name, maskController);
            Log.Logger.Log($"Created mask [{name}].");
        }
    }
}
