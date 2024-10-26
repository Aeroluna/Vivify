using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using Heck.Animation.Transform;
using Heck.Deserialize;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.XR;
using Vivify.ObjectPrefab.Managers;
using static Heck.HeckController;
using static Vivify.VivifyController;

namespace Vivify;

internal enum PostProcessingOrder
{
    BeforeMainEffect,
    AfterMainEffect
}

// TODO: implement unused enums
// ReSharper disable UnusedMember.Global
internal enum MaterialPropertyType
{
    Texture,
    Color,
    Float,
    FloatArray,
    Int,
    Vector,
    VectorArray
}

internal enum AnimatorPropertyType
{
    Bool,
    Float,
    Integer,
    Trigger
}

internal class VivifyObjectData : IObjectCustomData, ICopyable<IObjectCustomData>
{
    internal VivifyObjectData(
        CustomData customData,
        Dictionary<string, Track> beatmapTracks)
    {
        Track = customData.GetNullableTrackArray(beatmapTracks, false)?.ToList();
    }

    internal VivifyObjectData(
        VivifyObjectData original)
    {
        Track = original.Track;
    }

    internal IReadOnlyList<Track>? Track { get; }

    public IObjectCustomData Copy()
    {
        return new VivifyObjectData(this);
    }
}

internal class ApplyPostProcessingData : ICustomEventCustomData
{
    internal ApplyPostProcessingData(CustomData customData, Dictionary<string, List<object>> pointDefinitions)
    {
        Easing = customData.GetStringToEnum<Functions?>(EASING) ?? Functions.easeLinear;
        Duration = customData.GetRequired<float>(DURATION);
        Priority = customData.Get<int?>(PRIORITY) ?? 0;
        Source = customData.Get<string?>(SOURCE);
        Asset = customData.Get<string?>(ASSET);
        Pass = customData.Get<int?>(PASS);
        Order = customData.GetStringToEnum<PostProcessingOrder?>(ORDER) ?? PostProcessingOrder.AfterMainEffect;
        List<object>? properties = customData.Get<List<object>>(PROPERTIES);
        if (properties != null)
        {
            Properties = properties
                .Select(n => MaterialProperty.CreateMaterialProperty((CustomData)n, pointDefinitions))
                .ToList();
        }

        object? destRaw = customData.Get<object>(DESTINATION);
        Target = destRaw switch
        {
            null => null,
            string destString => [destString],
            List<object> destArray => destArray.Select(n => (string)n).ToArray(),
            _ => throw new InvalidCastException(
                $"[{DESTINATION}] was not an allowable type. Was [{destRaw.GetType().FullName}].")
        };
    }

    internal string? Asset { get; }

    internal float Duration { get; }

    internal Functions Easing { get; }

    internal int? Pass { get; }

    internal PostProcessingOrder Order { get; }

    internal int Priority { get; }

    internal List<MaterialProperty>? Properties { get; }

    internal string? Source { get; }

    internal string[]? Target { get; }
}

internal class SetRenderingSettingsData : ICustomEventCustomData
{
    internal SetRenderingSettingsData(
        CustomData customData,
        Dictionary<string, List<object>> pointDefinitions)
    {
        Duration = customData.Get<float?>(DURATION) ?? 0f;
        Easing = customData.GetStringToEnum<Functions?>(EASING) ?? Functions.easeLinear;

        string[] excludedStrings = [DURATION, EASING];
        List<KeyValuePair<string, object?>> categories =
            customData.Where(n => excludedStrings.All(m => m != n.Key)).ToList();
        foreach ((string category, object? propertiesRaw) in categories)
        {
            CustomData properties = (CustomData?)propertiesRaw ?? throw new InvalidOperationException();
            foreach ((string key, object? value) in properties)
            {
                if (value == null)
                {
                    continue;
                }

                RenderingSettingsProperty property;
                switch (category)
                {
                    case RENDER_SETTINGS:
                        switch (key)
                        {
                            case nameof(RenderSettings.ambientIntensity):
                            case nameof(RenderSettings.ambientMode):
                            case nameof(RenderSettings.defaultReflectionMode):
                            case nameof(RenderSettings.defaultReflectionResolution):
                            case nameof(RenderSettings.flareFadeSpeed):
                            case nameof(RenderSettings.flareStrength):
                            case nameof(RenderSettings.fog):
                            case nameof(RenderSettings.fogDensity):
                            case nameof(RenderSettings.fogEndDistance):
                            case nameof(RenderSettings.fogMode):
                            case nameof(RenderSettings.fogStartDistance):
                            case nameof(RenderSettings.haloStrength):
                            case nameof(RenderSettings.reflectionBounces):
                            case nameof(RenderSettings.reflectionIntensity):
                                property = RenderingSettingsProperty.CreateRenderSettingProperty<float>(
                                    key,
                                    value,
                                    properties,
                                    pointDefinitions);
                                break;

                            case nameof(RenderSettings.ambientEquatorColor):
                            case nameof(RenderSettings.ambientGroundColor):
                            case nameof(RenderSettings.ambientLight):
                            case nameof(RenderSettings.ambientSkyColor):
                            case nameof(RenderSettings.fogColor):
                            case nameof(RenderSettings.subtractiveShadowColor):
                                property = RenderingSettingsProperty.CreateRenderSettingProperty<Vector4>(
                                    key,
                                    value,
                                    properties,
                                    pointDefinitions);
                                break;

                            case nameof(RenderSettings.skybox):
                            case nameof(RenderSettings.sun):
                                property = new RenderingSettingsProperty<string>(
                                    key,
                                    properties.GetRequired<string>(key));
                                break;

                            default:
                                continue;
                        }

                        break;

                    case QUALITY_SETTINGS:
                        switch (key)
                        {
                            case nameof(QualitySettings.anisotropicFiltering):
                            case nameof(QualitySettings.antiAliasing):
                            case nameof(QualitySettings.pixelLightCount):
                            case nameof(QualitySettings.realtimeReflectionProbes):
                            case nameof(QualitySettings.shadowCascades):
                            case nameof(QualitySettings.shadowDistance):
                            case nameof(QualitySettings.shadowmaskMode):
                            case nameof(QualitySettings.shadowNearPlaneOffset):
                            case nameof(QualitySettings.shadowProjection):
                            case nameof(QualitySettings.shadowResolution):
                            case nameof(QualitySettings.shadows):
                            case nameof(QualitySettings.softParticles):
                                property = RenderingSettingsProperty.CreateRenderSettingProperty<float>(
                                    key,
                                    value,
                                    properties,
                                    pointDefinitions);
                                break;

                            default:
                                continue;
                        }

                        break;

                    case XR_SETTINGS:
                        switch (key)
                        {
                            case nameof(XRSettings.useOcclusionMesh):
                                property = RenderingSettingsProperty.CreateRenderSettingProperty<float>(
                                    key,
                                    value,
                                    properties,
                                    pointDefinitions);
                                break;

                            default:
                                continue;
                        }

                        break;

                    default:
                        continue;
                }

                Properties.Add(property);
            }
        }
    }

    internal float Duration { get; }

    internal Functions Easing { get; }

    internal List<RenderingSettingsProperty> Properties { get; } = [];
}

internal class CreateCameraData : ICustomEventCustomData
{
    internal CreateCameraData(CustomData customData, Dictionary<string, Track> tracks)
    {
        Name = customData.GetRequired<string>(ID_FIELD);
        Texture = customData.GetRequired<string>(TEXTURE);
        DepthTexture = customData.Get<string?>(DEPTH_TEXTURE);
        CustomData? propertyData = customData.Get<CustomData?>(PROPERTIES);
        if (propertyData != null)
        {
            Property = CameraProperty.CreateCameraProperty(propertyData, tracks);
        }
    }

    internal string Name { get; }

    internal string Texture { get; }

    internal string? DepthTexture { get; }

    internal CameraProperty? Property { get; }
}

internal class CreateScreenTextureData : ICustomEventCustomData
{
    internal CreateScreenTextureData(CustomData customData)
    {
        Name = customData.GetRequired<string>(ID_FIELD);
        PropertyId = Shader.PropertyToID(Name);
        XRatio = customData.Get<float?>(X_RATIO) ?? 1;
        YRatio = customData.Get<float?>(Y_RATIO) ?? 1;
        Width = customData.Get<int?>(WIDTH);
        Height = customData.Get<int?>(HEIGHT);
        Format = customData.GetStringToEnum<RenderTextureFormat?>(FORMAT);
        FilterMode = customData.GetStringToEnum<FilterMode?>(FILTER);
        if (Format.HasValue && !SystemInfo.SupportsRenderTextureFormat(Format.Value))
        {
            Plugin.Log.Warn($"Current graphics card does not support [{Format.Value}]");
        }
    }

    internal FilterMode? FilterMode { get; }

    internal RenderTextureFormat? Format { get; }

    internal int? Height { get; }

    internal string Name { get; }

    internal int PropertyId { get; }

    internal int? Width { get; }

    internal float XRatio { get; }

    internal float YRatio { get; }
}

internal class DestroyObjectData : ICustomEventCustomData
{
    internal DestroyObjectData(CustomData customData)
    {
        object nameRaw = customData.GetRequired<object>(ID_FIELD);
        Id = nameRaw switch
        {
            string nameString => [nameString],
            List<object> nameArray => nameArray.Select(n => (string)n).ToArray(),
            _ => throw new InvalidCastException(
                $"[{ID_FIELD}] was not an allowable type. Was [{nameRaw.GetType().FullName}].")
        };
    }

    internal string[] Id { get; }
}

internal class InstantiatePrefabData : ICustomEventCustomData
{
    internal InstantiatePrefabData(
        CustomData customData,
        Dictionary<string, Track> beatmapTracks)
    {
        Asset = customData.GetRequired<string>(ASSET);
        Id = customData.Get<string>(ID_FIELD);
        TransformData = new TransformData(customData);
        Track = customData.GetNullableTrackArray(beatmapTracks, false)?.ToList();
    }

    internal string Asset { get; }

    internal string? Id { get; }

    internal List<Track>? Track { get; }

    internal TransformData TransformData { get; }
}

internal class AssignObjectPrefabData : ICustomEventCustomData
{
    internal AssignObjectPrefabData(
        CustomData customData,
        Dictionary<string, Track> beatmapTracks)
    {
        LoadMode = customData.GetStringToEnum<LoadMode>(ASSIGN_PREFAB_LOAD_MODE);
        foreach ((string key, object? value) in customData.Where(n => n.Key != ASSIGN_PREFAB_LOAD_MODE))
        {
            CustomData objectData =
                (CustomData?)value ?? throw new InvalidOperationException($"Null value for [{key}]");

            string? baseString = string.Empty;
            if (objectData.TryGetValue(ASSET, out object? baseAsset))
            {
                baseString = (string?)baseAsset;
            }

            switch (key)
            {
                case SABER_PREFAB:
                    string? trailString = string.Empty;
                    if (objectData.TryGetValue(SABER_TRAIL_ASSET, out object? trailAsset))
                    {
                        trailString = (string?)trailAsset;
                    }

                    Assets.Add(
                        key,
                        new SaberPrefabInfo(
                            objectData.GetStringToEnumRequired<SaberPrefabInfo.SaberType>(SABER_TYPE),
                            baseString,
                            trailString,
                            objectData.GetVector3(SABER_TRAIL_TOP_POS),
                            objectData.GetVector3(SABER_TRAIL_BOTTOM_POS),
                            objectData.Get<float?>(SABER_TRAIL_DURATION),
                            objectData.Get<int?>(SABER_TRAIL_SAMPLE_FREQ),
                            objectData.Get<int?>(SABER_TRAIL_GRANULARITY)));
                    break;

                default:
                    string? debrisString = string.Empty;
                    if (objectData.TryGetValue(DEBRIS_ASSET, out object? debrisAsset))
                    {
                        debrisString = (string?)debrisAsset;
                    }

                    string? anyDirectionString = string.Empty;
                    if (objectData.TryGetValue(ANY_ASSET, out object? anyDirectionAsset))
                    {
                        anyDirectionString = (string?)anyDirectionAsset;
                    }

                    List<Track> tracks = objectData.GetTrackArray(beatmapTracks, false).ToList();

                    Assets.Add(key, new ObjectPrefabInfo(baseString, debrisString, anyDirectionString, tracks));
                    break;
            }
        }
    }

    internal interface IPrefabInfo;

    internal Dictionary<string, IPrefabInfo> Assets { get; } = new();

    internal LoadMode LoadMode { get; }

    internal struct ObjectPrefabInfo : IPrefabInfo
    {
        internal ObjectPrefabInfo(
            string? asset,
            string? debrisAsset,
            string? anyDirectionAsset,
            IReadOnlyList<Track> track)
        {
            Asset = asset;
            DebrisAsset = debrisAsset;
            AnyDirectionAsset = anyDirectionAsset;
            Track = track;
        }

        internal string? Asset { get; }

        internal string? DebrisAsset { get; }

        internal string? AnyDirectionAsset { get; }

        internal IReadOnlyList<Track> Track { get; }
    }

    internal struct SaberPrefabInfo : IPrefabInfo
    {
        internal SaberPrefabInfo(
            SaberType type,
            string? asset,
            string? trailAsset,
            Vector3? topPos,
            Vector3? bottomPos,
            float? duration,
            int? samplingFrequency,
            int? granularity)
        {
            Type = type;
            Asset = asset;
            TrailAsset = trailAsset;
            TopPos = topPos;
            BottomPos = bottomPos;
            Duration = duration;
            SamplingFrequency = samplingFrequency;
            Granularity = granularity;
        }

        [Flags]
        internal enum SaberType
        {
            Left = 1,
            Right = 2,
            Both = Left | Right
        }

        internal SaberType Type { get; }

        internal string? Asset { get; }

        internal string? TrailAsset { get; }

        internal Vector3? TopPos { get; }

        internal Vector3? BottomPos { get; }

        internal float? Duration { get; }

        internal int? SamplingFrequency { get; }

        internal int? Granularity { get; }
    }
}
