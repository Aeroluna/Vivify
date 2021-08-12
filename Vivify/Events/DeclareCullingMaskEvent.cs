namespace Vivify.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using Heck.Animation;
    using UnityEngine;
    using Vivify.PostProcessing;

    internal static class DeclareCullingMaskEvent
    {
        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type == "DeclareCullingMask")
            {
                string name = customEventData.data.Get<string>("_name") ?? throw new InvalidOperationException("Mask name not found.");
                IEnumerable<Track> tracks = AnimationHelper.GetTrackArray(customEventData.data, EventController.Instance!.CustomEventCallbackController!.BeatmapData!)
                    ?? throw new InvalidOperationException("No tracks found.");
                bool whitelist = customEventData.data.Get<bool?>("_whitelist") ?? false;
                CullingMaskController maskController = new CullingMaskController(tracks, whitelist);
                PostProcessingController.CullingMasks.Add(name, maskController);
                Plugin.Logger.Log($"Created culling mask [{name}].");
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
}
