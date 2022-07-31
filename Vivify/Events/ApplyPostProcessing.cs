using System.Collections;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using UnityEngine;
using Vivify.PostProcessing;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void ApplyPostProcessing(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out ApplyPostProcessingData? heckData))
            {
                return;
            }

            float duration = 60f * heckData.Duration / _bpmController.currentBpm; // Convert to real time;
            string? assetName = heckData.Asset;
            Material? material = null;
            if (assetName != null)
            {
                if (!AssetBundleController.Assets.TryGetValue(assetName, out Object gameObject))
                {
                    Log.Logger.Log($"Could not find material [{assetName}].", Logger.Level.Error);
                    return;
                }

                if (gameObject is not Material casted)
                {
                    Log.Logger.Log($"Found [{assetName}], but was not material!", Logger.Level.Error);
                    return;
                }

                material = casted;
                List<MaterialProperty>? properties = heckData.Properties;
                if (properties != null)
                {
                    SetMaterialProperties(material, properties, duration, heckData.Easing, customEventData.time);
                }
            }

            if (duration == 0 || _audioTimeSource.songTime > customEventData.time + duration)
            {
                return;
            }

            MaterialData materialData = new(material, heckData.Priority, heckData.Target, heckData.Pass);
            PostProcessingController.PostProcessingMaterial.Add(materialData);
            Log.Logger.Log($"Applied post processing material [{assetName}] for [{duration}] seconds.");
            _coroutineDummy.StartCoroutine(KillPostProcessingCoroutine(materialData, duration, customEventData.time));
        }

        internal IEnumerator KillPostProcessingCoroutine(MaterialData data, float duration, float startTime)
        {
            while (true)
            {
                float elapsedTime = _audioTimeSource.songTime - startTime;
                if (elapsedTime < 0)
                {
                    break;
                }

                if (elapsedTime < duration)
                {
                    yield return null;
                }
                else
                {
                    PostProcessingController.PostProcessingMaterial.Remove(data);
                    break;
                }
            }
        }
    }
}
