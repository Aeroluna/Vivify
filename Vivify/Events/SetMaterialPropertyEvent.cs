namespace Vivify.Events
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using Heck.Animation;
    using UnityEngine;

    internal static class SetMaterialPropertyEvent
    {
        internal enum MaterialProperty
        {
            Texture,
            Color,
            Float,
            FloatArray,
            Int,
            Vector,
            VectorArray,
        }

        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type == "SetMaterialProperty")
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

                Material material = AssetBundleController.TryGetAsset<Material>(assetName);
                if (material != null)
                {
                    dynamic properties = Trees.at(customEventData.data, "_properties");
                    SetMaterialProperties(material, properties, duration, easing, customEventData.time);
                }
            }
        }

        internal static void SetMaterialProperties(Material material, dynamic propertyData, float duration, Functions easing, float startTime)
        {
            List<object> properties = (List<object>)propertyData;
            foreach (dynamic property in properties)
            {
                string name = Trees.at(property, "_name");
                MaterialProperty type = Enum.Parse(typeof(MaterialProperty), Trees.at(property, "_type"));
                object value = Trees.at(property, "_value");

                switch (type)
                {
                    case MaterialProperty.Texture:
                        string texValue = Convert.ToString(value);
                        if (Enum.TryParse(texValue, out TextureRequest textureRequest))
                        {
                            AssetBundleController.MaterialData[material].TextureRequests.Add(name, textureRequest);

                            int requestId = (int)textureRequest;
                            if (requestId >= 0)
                            {
                                material.SetTexture(name, PostProcessingController.MainRenderTextures[requestId]);
                            }
                        }
                        else
                        {
                            Texture texture = AssetBundleController.TryGetAsset<Texture>(texValue);
                            if (texture != null)
                            {
                                material.SetTexture(name, texture);
                            }
                        }

                        break;

                    case MaterialProperty.Color:
                        if (value is List<object>)
                        {
                            EventController.Instance.StartCoroutine(AnimatePropertyCoroutine(GetPointDefinition(property, "_value"), material, name, MaterialProperty.Color, duration, startTime, easing));
                        }
                        else
                        {
                            IEnumerable<float> color = ((List<object>)value).Select(n => Convert.ToSingle(n));

                            material.SetColor(name, new Color(color.ElementAt(0), color.ElementAt(1), color.ElementAt(2), color.Count() > 3 ? color.ElementAt(3) : 1));
                        }

                        break;

                    case MaterialProperty.Float:
                        if (value is List<object>)
                        {
                            EventController.Instance.StartCoroutine(AnimatePropertyCoroutine(GetPointDefinition(property, "_value"), material, name, MaterialProperty.Float, duration, startTime, easing));
                        }
                        else
                        {
                            Plugin.Logger.Log(value.GetType());
                            material.SetFloat(name, Convert.ToSingle(value));
                        }

                        break;

                    default:
                        // im lazy, shoot me
                        Plugin.Logger.Log($"{type} not currently supported", IPA.Logging.Logger.Level.Warning);
                        break;
                }
            }
        }

        internal static IEnumerator AnimatePropertyCoroutine(PointDefinition points, Material material, string name, MaterialProperty type, float duration, float startTime, Functions easing)
        {
            while (true)
            {
                float elapsedTime = EventController.Instance.CustomEventCallbackController._audioTimeSource.songTime - startTime;
                float time = Easings.Interpolate(Mathf.Min(elapsedTime / duration, 1f), easing);
                switch (type)
                {
                    case MaterialProperty.Color:
                        material.SetColor(name, points.InterpolateVector4(time));
                        break;

                    case MaterialProperty.Float:
                        material.SetFloat(name, points.InterpolateLinear(time));
                        break;

                    case MaterialProperty.Vector:
                        material.SetVector(name, points.InterpolateVector4(time));
                        break;
                }

                if (elapsedTime < duration)
                {
                    yield return null;
                }
                else
                {
                    break;
                }
            }
        }

        private static PointDefinition GetPointDefinition(dynamic data, string name)
        {
            Dictionary<string, PointDefinition> pointDefinitions = Trees.at(((CustomBeatmapData)EventController.Instance.CustomEventCallbackController._beatmapData).customData, "pointDefinitions");
            AnimationHelper.TryGetPointData(data, name, out PointDefinition pointData, pointDefinitions);
            return pointData;
        }
    }
}
