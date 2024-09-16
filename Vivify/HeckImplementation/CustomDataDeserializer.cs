using System;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using Heck.Deserialize;
using static Vivify.VivifyController;

namespace Vivify;

internal class CustomDataDeserializer : IEarlyDeserializer, IObjectsDeserializer, ICustomEventsDeserializer
{
    private readonly CustomBeatmapData _beatmapData;
    private readonly float _bpm;
    private readonly Dictionary<string, List<object>> _pointDefinitions;
    private readonly TrackBuilder _trackBuilder;
    private readonly Dictionary<string, Track> _tracks;

    private CustomDataDeserializer(
        TrackBuilder trackBuilder,
        CustomBeatmapData beatmapData,
        Dictionary<string, List<object>> pointDefinitions,
        Dictionary<string, Track> tracks,
        float bpm)
    {
        _trackBuilder = trackBuilder;
        _beatmapData = beatmapData;
        _pointDefinitions = pointDefinitions;
        _tracks = tracks;
        _bpm = bpm;
    }

    public Dictionary<CustomEventData, ICustomEventCustomData> DeserializeCustomEvents()
    {
        Dictionary<CustomEventData, ICustomEventCustomData> dictionary = new();
        foreach (CustomEventData customEventData in _beatmapData.customEventDatas)
        {
            try
            {
                CustomData data = customEventData.customData;
                switch (customEventData.eventType)
                {
                    case APPLY_POST_PROCESSING:
                        dictionary.Add(customEventData, new ApplyPostProcessingData(data, _pointDefinitions));
                        break;

                    case ASSIGN_OBJECT_PREFAB:
                        dictionary.Add(customEventData, new AssignObjectPrefabData(data, _tracks));
                        break;

                    case DECLARE_CULLING_TEXTURE:
                        dictionary.Add(customEventData, new DeclareCullingTextureData(data, _tracks));
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
                        dictionary.Add(customEventData, new InstantiatePrefabData(data, _tracks));
                        break;

                    case SET_MATERIAL_PROPERTY:
                        dictionary.Add(customEventData, new SetMaterialPropertyData(data, _pointDefinitions));
                        break;

                    case SET_GLOBAL_PROPERTY:
                        dictionary.Add(customEventData, new SetGlobalPropertyData(data, _pointDefinitions));
                        break;

                    case SET_CAMERA_PROPERTY:
                        dictionary.Add(customEventData, new SetCameraPropertyData(data));
                        break;

                    case SET_ANIMATOR_PROPERTY:
                        dictionary.Add(customEventData, new SetAnimatorPropertyData(data, _pointDefinitions));
                        break;

                    case SET_RENDERING_SETTINGS:
                        dictionary.Add(customEventData, new SetRenderingSettingsData(data, _pointDefinitions));
                        break;

                    default:
                        continue;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.DeserializeFailure(e, customEventData, _bpm);
            }
        }

        return dictionary;
    }

    public void DeserializeEarly()
    {
        foreach (CustomEventData customEventData in _beatmapData.customEventDatas)
        {
            try
            {
                switch (customEventData.eventType)
                {
                    case INSTANTIATE_PREFAB:
                        _trackBuilder.AddFromCustomData(customEventData.customData, false, false);
                        break;

                    default:
                        continue;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.DeserializeFailure(e, customEventData, _bpm);
            }
        }
    }

    public Dictionary<BeatmapObjectData, IObjectCustomData> DeserializeObjects()
    {
        Dictionary<BeatmapObjectData, IObjectCustomData> dictionary = new();

        foreach (BeatmapObjectData beatmapObjectData in _beatmapData.beatmapObjectDatas)
        {
            try
            {
                CustomData customData = ((ICustomData)beatmapObjectData).customData;
                dictionary.Add(beatmapObjectData, new VivifyObjectData(customData, _tracks));
            }
            catch (Exception e)
            {
                Plugin.Log.DeserializeFailure(e, beatmapObjectData, _bpm);
            }
        }

        return dictionary;
    }
}
