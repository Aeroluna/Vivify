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
        private static readonly Coroutine?[] _activeCoroutine = new Coroutine[PostProcessingController.TEXTURECOUNT];

        internal void ApplyPostProcessing(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out ApplyPostProcessingData? heckData))
            {
                return;
            }

            float duration = 60f * heckData.Duration / _bpmController.currentBpm; // Convert to real time;
            int pass = heckData.Pass;
            string assetName = heckData.Asset;
            if (!AssetBundleController.Assets.TryGetValue(assetName, out Object gameObject))
            {
                Log.Logger.Log($"Could not find material [{assetName}].", Logger.Level.Error);
                return;
            }

            if (gameObject is not Material material)
            {
                Log.Logger.Log($"Found [{assetName}], but was not material!", Logger.Level.Error);
                return;
            }

            List<MaterialProperty>? properties = heckData.Properties;
            if (properties != null)
            {
                SetMaterialProperties(material, properties, duration, heckData.Easing, customEventData.time);
            }

            MaterialData materialData = AssetBundleController.MaterialData[material];
            PostProcessingController.PostProcessingMaterial[pass] = materialData;

            if (_activeCoroutine[pass] != null)
            {
                _coroutineDummy.StopCoroutine(_activeCoroutine[pass]);
            }

            Log.Logger.Log($"Applied post processing material [{assetName}] for [{duration}] seconds.");
            _activeCoroutine[pass] = _coroutineDummy.StartCoroutine(KillPostProcessingCoroutine(pass, duration, customEventData.time));
        }

        internal IEnumerator KillPostProcessingCoroutine(int pass, float duration, float startTime)
        {
            while (true)
            {
                float elapsedTime = _audioTimeSource.songTime - startTime;
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
