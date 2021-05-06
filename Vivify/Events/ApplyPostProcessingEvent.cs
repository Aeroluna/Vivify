namespace Vivify.Events
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using UnityEngine;
    using Vivify.HarmonyPatches;
    using Heck.Animation;

    internal static class ApplyPostProcessingEvent
    {
        private static Coroutine _activeCoroutine;

        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type == "ApplyPostProcessing")
            {
                string easingString = (string)Trees.at(customEventData.data, "_easing");
                Functions easing = Functions.easeLinear;
                if (easingString != null)
                {
                    easing = (Functions)Enum.Parse(typeof(Functions), easingString);
                }

                float duration = (float?)Trees.at(customEventData.data, "_duration") ?? 0f;
                duration = 60f * duration / EventController.Instance.BeatmapObjectSpawnController.currentBpm; // Convert to real time;

                string assetName = Trees.at(customEventData.data, "_asset");
                if (AssetBundleController.Assets.TryGetValue(assetName, out UnityEngine.Object gameObject))
                {
                    if (gameObject is Material material)
                    {
                        PostProcessingController.PostProcessingMaterial = material;
                        if (_activeCoroutine != null)
                        {
                            EventController.Instance.StopCoroutine(_activeCoroutine);
                        }

                        _activeCoroutine = EventController.Instance.StartCoroutine(KillPostProcessingCoroutine(duration, customEventData.time));

                        dynamic properties = Trees.at(customEventData.data, "_properties");
                        if (properties != null)
                        {
                            SetMaterialPropertyEvent.SetMaterialProperties(material, properties, duration, easing, customEventData.time);
                        }
                    }
                    else
                    {
                        Plugin.Logger.Log($"Found {assetName}, but was not material!", IPA.Logging.Logger.Level.Error);
                    }
                }
                else
                {
                    Plugin.Logger.Log($"Could not find material {assetName}", IPA.Logging.Logger.Level.Error);
                }
            }
        }

        internal static IEnumerator KillPostProcessingCoroutine(float duration, float startTime)
        {
            while (true)
            {
                float elapsedTime = EventController.Instance.CustomEventCallbackController._audioTimeSource.songTime - startTime;
                if (elapsedTime < duration)
                {
                    yield return null;
                }
                else
                {
                    PostProcessingController.PostProcessingMaterial = null;
                    break;
                }
            }
        }
    }
}
