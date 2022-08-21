using System;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
using static Vivify.VivifyController;

namespace Vivify
{
    internal class CustomDataManager
    {
        [EarlyDeserializer]
        internal static void DeserializerEarly(
            TrackBuilder trackBuilder,
            CustomBeatmapData beatmapData,
            IReadOnlyList<CustomEventData> customEventDatas)
        {
            foreach (CustomEventData customEventData in customEventDatas)
            {
                try
                {
                    switch (customEventData.eventType)
                    {
                        case INSTANTIATE_PREFAB:
                            trackBuilder.AddFromCustomData(customEventData.customData, false, false);
                            break;

                        default:
                            continue;
                    }
                }
                catch (Exception e)
                {
                    Log.Logger.LogFailure(e, customEventData);
                }
            }
        }

        [CustomEventsDeserializer]
        private static Dictionary<CustomEventData, ICustomEventCustomData> DeserializeCustomEvents(
            CustomBeatmapData beatmapData,
            Dictionary<string, List<object>> pointDefinitions,
            Dictionary<string, Track> tracks,
            IReadOnlyList<CustomEventData> customEventDatas)
        {
            Dictionary<CustomEventData, ICustomEventCustomData> dictionary = new();
            foreach (CustomEventData customEventData in customEventDatas)
            {
                try
                {
                    CustomData data = customEventData.customData;
                    switch (customEventData.eventType)
                    {
                        case APPLY_POST_PROCESSING:
                            dictionary.Add(customEventData, new ApplyPostProcessingData(data, pointDefinitions));
                            break;

                        case DECLARE_CULLING_MASK:
                            dictionary.Add(customEventData, new DeclareCullingMaskData(data, tracks));
                            break;

                        case DECLARE_TEXTURE:
                            dictionary.Add(customEventData, new DeclareRenderTextureData(data));
                            break;

                        case DESTROY_PREFAB:
                            dictionary.Add(customEventData, new DestroyPrefabData(data));
                            break;

                        case INSTANTIATE_PREFAB:
                            dictionary.Add(customEventData, new InstantiatePrefabData(data, tracks));
                            break;

                        case SET_MATERIAL_PROPERTY:
                            dictionary.Add(customEventData, new SetMaterialPropertyData(data, pointDefinitions));
                            break;

                        default:
                            continue;
                    }
                }
                catch (Exception e)
                {
                    Log.Logger.LogFailure(e, customEventData);
                }
            }

            return dictionary;
        }
    }
}
