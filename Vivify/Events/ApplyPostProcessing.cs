using System.Collections;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using UnityEngine;
using Vivify.PostProcessing;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void ApplyPostProcessing(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out ApplyPostProcessingData? data))
            {
                return;
            }

            float duration = 60f * data.Duration / _bpmController.currentBpm; // Convert to real time;
            string? assetName = data.Asset;
            Material? material = null;
            if (assetName != null)
            {
                if (!_assetBundleManager.TryGetAsset(assetName, out material))
                {
                    return;
                }

                List<MaterialProperty>? properties = data.Properties;
                if (properties != null)
                {
                    SetMaterialProperties(material, properties, duration, data.Easing, customEventData.time);
                }
            }

            if (duration == 0)
            {
                PostProcessingController.PostProcessingMaterial.Add(new MaterialData(material, data.Priority, data.Source, data.Target, data.Pass, Time.frameCount));
                Log.Logger.Log($"Applied material [{assetName}] for single frame.");
                return;
            }

            if (duration <= 0 || _audioTimeSource.songTime > customEventData.time + duration)
            {
                return;
            }

            MaterialData materialData = new(material, data.Priority, data.Source, data.Target, data.Pass);
            PostProcessingController.PostProcessingMaterial.Add(materialData);
            Log.Logger.Log($"Applied material [{assetName}] for [{duration}] seconds.");
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
