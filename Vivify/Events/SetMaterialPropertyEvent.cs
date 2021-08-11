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
                string? easingString = customEventData.data.Get<string>("_easing");
                Functions easing = Functions.easeLinear;
                if (easingString != null)
                {
                    easing = (Functions)Enum.Parse(typeof(Functions), easingString);
                }

                float duration = customEventData.data.Get<float?>("_duration") ?? 0f;
                duration = 60f * duration / EventController.Instance!.BeatmapObjectSpawnController!.currentBpm; // Convert to real time;

                string assetName = customEventData.data.Get<string>("_asset") ?? throw new InvalidOperationException("Asset name not found.");

                Material? material = AssetBundleController.TryGetAsset<Material>(assetName);
                if (material != null)
                {
                    List<object> properties = customEventData.data.Get<List<object>>("_properties") ?? throw new InvalidOperationException("No properties found.");
                    SetMaterialProperties(material, properties, duration, easing, customEventData.time);
                }
            }
        }

        internal static void SetMaterialProperties(Material material, List<object> properties, float duration, Functions easing, float startTime)
        {
            foreach (Dictionary<string, object?> property in properties)
            {
                string name = property.Get<string>("_name") ?? throw new InvalidOperationException("Property name not found.");
                MaterialProperty type = (MaterialProperty)Enum.Parse(typeof(MaterialProperty), property.Get<string>("_type"));
                object value = property.Get<object>("_value") ?? throw new InvalidOperationException("Property value not found.");

                switch (type)
                {
                    case MaterialProperty.Texture:
                        string texValue = Convert.ToString(value);
                        Texture? texture = AssetBundleController.TryGetAsset<Texture>(texValue);
                        if (texture != null)
                        {
                            material.SetTexture(name, texture);
                        }

                        break;

                    case MaterialProperty.Color:
                        if (value is List<object>)
                        {
                            EventController.Instance!.StartCoroutine(AnimatePropertyCoroutine(GetPointDefinition(property, "_value"), material, name, MaterialProperty.Color, duration, startTime, easing));
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
                            EventController.Instance!.StartCoroutine(AnimatePropertyCoroutine(GetPointDefinition(property, "_value"), material, name, MaterialProperty.Float, duration, startTime, easing));
                        }
                        else
                        {
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
                float elapsedTime = EventController.Instance!.CustomEventCallbackController!.AudioTimeSource!.songTime - startTime;
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

        private static PointDefinition GetPointDefinition(Dictionary<string, object?> data, string name)
        {
            Dictionary<string, PointDefinition> pointDefinitions = EventController.Instance!.CustomEventCallbackController!.BeatmapData!.customData.Get<Dictionary<string, PointDefinition>>("pointDefinitions")
                ?? throw new InvalidOperationException("Could not get BeatmapData point definitions.");
            AnimationHelper.TryGetPointData(data, name, out PointDefinition? pointData, pointDefinitions);
            return pointData ?? throw new InvalidOperationException("Failed to create point definition.");
        }
    }
}
