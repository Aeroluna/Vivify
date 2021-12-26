using System;
using System.Collections;
using System.Collections.Generic;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using UnityEngine;
using Vivify.PostProcessing;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace Vivify.Events
{
    internal static class ApplyPostProcessingEvent
    {
        private static readonly Coroutine?[] _activeCoroutine = new Coroutine[PostProcessingController.TEXTURECOUNT];

        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type != "ApplyPostProcessing")
            {
                return;
            }

            string? easingString = customEventData.data.Get<string>("_easing");
            Functions easing = Functions.easeLinear;
            if (easingString != null)
            {
                easing = (Functions)Enum.Parse(typeof(Functions), easingString);
            }

            float duration = customEventData.data.Get<float?>("_duration") ?? 0f;
            duration = 60f * duration / EventController.BeatmapObjectSpawnController.currentBpm; // Convert to real time;

            int pass = customEventData.data.Get<int?>("_pass") ?? 0;

            string assetName = customEventData.data.Get<string>("_asset") ?? throw new InvalidOperationException("Asset name not found.");
            if (AssetBundleController.Assets.TryGetValue(assetName, out Object gameObject))
            {
                if (gameObject is Material material)
                {
                    List<object>? properties = customEventData.data.Get<List<object>>("_properties");
                    if (properties != null)
                    {
                        SetMaterialPropertyEvent.SetMaterialProperties(material, properties, duration, easing, customEventData.time);
                    }

                    MaterialData materialData = AssetBundleController.MaterialData[material];
                    PostProcessingController.PostProcessingMaterial[pass] = materialData;

                    if (_activeCoroutine[pass] != null)
                    {
                        EventController.Instance.StopCoroutine(_activeCoroutine[pass]);
                    }

                    Log.Logger.Log($"Applied post processing material [{assetName}] for [{duration}] seconds.");
                    _activeCoroutine[pass] = EventController.Instance.StartCoroutine(KillPostProcessingCoroutine(pass, duration, customEventData.time));
                }
                else
                {
                    Log.Logger.Log($"Found {assetName}, but was not material!", Logger.Level.Error);
                }
            }
            else
            {
                Log.Logger.Log($"Could not find material {assetName}", Logger.Level.Error);
            }
        }

        internal static IEnumerator KillPostProcessingCoroutine(int pass, float duration, float startTime)
        {
            while (true)
            {
                float elapsedTime = EventController.Instance.CustomEventCallbackController.AudioTimeSource!.songTime - startTime;
                if (elapsedTime < duration)
                {
                    yield return null;
                }
                else
                {
                    PostProcessingController.PostProcessingMaterial[pass] = null;
                    break;
                }
            }
        }
    }
}
