using System;
using System.Collections.Generic;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using Vivify.PostProcessing;
using Vivify.PostProcessing.TrackGameObject;

namespace Vivify.Events
{
    internal static class DeclareCullingMaskEvent
    {
        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type != "DeclareCullingMask")
            {
                return;
            }

            string name = customEventData.data.Get<string>("_name") ?? throw new InvalidOperationException("Mask name not found.");
            IEnumerable<Track> tracks = AnimationHelper.GetTrackArray(customEventData.data, EventController.Instance.CustomEventCallbackController.BeatmapData!)
                                        ?? throw new InvalidOperationException("No tracks found.");
            bool whitelist = customEventData.data.Get<bool?>("_whitelist") ?? false;
            bool depthTexture = customEventData.data.Get<bool?>("_depthTexture") ?? false;
            CullingMaskController maskController = new(tracks, whitelist, depthTexture);
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
