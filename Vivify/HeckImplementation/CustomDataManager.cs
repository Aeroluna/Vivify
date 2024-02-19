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
            IDifficultyBeatmap difficultyBeatmap)
        {
            foreach (CustomEventData customEventData in beatmapData.customEventDatas)
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
                    Plugin.Log.DeserializeFailure(e, customEventData, difficultyBeatmap);
                }
            }
        }

        [ObjectsDeserializer]
        internal static Dictionary<BeatmapObjectData, IObjectCustomData> DeserializeObjects(
            CustomBeatmapData beatmapData,
            Dictionary<string, Track> beatmapTracks,
            IDifficultyBeatmap difficultyBeatmap)
        {
            bool v2 = beatmapData.version2_6_0AndEarlier;
            Dictionary<BeatmapObjectData, IObjectCustomData> dictionary = new();

            foreach (BeatmapObjectData beatmapObjectData in beatmapData.beatmapObjectDatas)
            {
                try
                {
                    CustomData customData = ((ICustomData)beatmapObjectData).customData;
                    dictionary.Add(beatmapObjectData, new VivifyObjectData(customData, beatmapTracks, v2));
                }
                catch (Exception e)
                {
                    Plugin.Log.DeserializeFailure(e, beatmapObjectData, difficultyBeatmap);
                }
            }

            return dictionary;
        }

        [CustomEventsDeserializer]
        private static Dictionary<CustomEventData, ICustomEventCustomData> DeserializeCustomEvents(
            CustomBeatmapData beatmapData,
            IDifficultyBeatmap difficultyBeatmap,
            Dictionary<string, List<object>> pointDefinitions,
            Dictionary<string, Track> tracks)
        {
            Dictionary<CustomEventData, ICustomEventCustomData> dictionary = new();
            foreach (CustomEventData customEventData in beatmapData.customEventDatas)
            {
                try
                {
                    CustomData data = customEventData.customData;
                    switch (customEventData.eventType)
                    {
                        case APPLY_POST_PROCESSING:
                            dictionary.Add(customEventData, new ApplyPostProcessingData(data, pointDefinitions));
                            break;

                        case ASSIGN_TRACK_PREFAB:
                            dictionary.Add(customEventData, new AssignTrackPrefabData(data, tracks));
                            break;

                        case DECLARE_CULLING_TEXTURE:
                            dictionary.Add(customEventData, new DeclareCullingMaskData(data, tracks));
                            break;

                        case DECLARE_TEXTURE:
                            dictionary.Add(customEventData, new DeclareRenderTextureData(data));
                            break;

                        case DESTROY_TEXTURE:
                            dictionary.Add(customEventData, new DestroyTextureData(data));
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

                        case SET_GLOBAL_PROPERTY:
                            dictionary.Add(customEventData, new SetGlobalPropertyData(data, pointDefinitions));
                            break;

                        case SET_CAMERA_PROPERTY:
                            dictionary.Add(customEventData, new SetCameraPropertyData(data));
                            break;

                        case SET_ANIMATOR_PROPERTY:
                            dictionary.Add(customEventData, new SetAnimatorPropertyData(data, pointDefinitions));
                            break;

                        case SET_RENDER_SETTING:
                            dictionary.Add(customEventData, new SetRenderSettingData(data, pointDefinitions));
                            break;

                        default:
                            continue;
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.DeserializeFailure(e, customEventData, difficultyBeatmap);
                }
            }

            return dictionary;
        }
    }
}
