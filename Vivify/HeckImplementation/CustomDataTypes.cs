using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using Heck.Animation.Transform;
using Heck.Deserialize;
using IPA.Utilities;
using UnityEngine;
using Vivify.ObjectPrefab.Managers;
using static Heck.HeckController;
using static Vivify.VivifyController;

namespace Vivify;

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

internal class AnimatedMaterialProperty<T> : MaterialProperty
    where T : struct
{
    internal AnimatedMaterialProperty(
        CustomData rawData,
        MaterialPropertyType materialPropertyType,
        object value,
        Dictionary<string, List<object>> pointDefinitions)
        : base(rawData, materialPropertyType, value)
    {
        PointDefinition = rawData.GetPointData<T>(VALUE, pointDefinitions) ??
                          throw new JsonNotDefinedException(VALUE);
    }

    internal PointDefinition<T> PointDefinition { get; }
}

internal class MaterialProperty
{
    internal MaterialProperty(CustomData rawData, MaterialPropertyType materialPropertyType, object value)
    {
        Name = Shader.PropertyToID(rawData.GetRequired<string>(ID_FIELD));
        Type = materialPropertyType;
        Value = value;
    }

    internal int Name { get; }

    internal MaterialPropertyType Type { get; }

    internal object Value { get; }

    internal static MaterialProperty CreateMaterialProperty(
        CustomData rawData,
        Dictionary<string, List<object>> pointDefinitions)
    {
        MaterialPropertyType type = rawData.GetStringToEnumRequired<MaterialPropertyType>(TYPE);
        object value = rawData.GetRequired<object>(VALUE);
        if (value is List<object>)
        {
            return type switch
            {
                MaterialPropertyType.Color => new AnimatedMaterialProperty<Vector4>(
                    rawData,
                    type,
                    value,
                    pointDefinitions),
                MaterialPropertyType.Float => new AnimatedMaterialProperty<float>(
                    rawData,
                    type,
                    value,
                    pointDefinitions),
                MaterialPropertyType.Vector => new AnimatedMaterialProperty<Vector4>(
                    rawData,
                    type,
                    value,
                    pointDefinitions),
                _ => throw new InvalidOperationException($"[{type}] not currently supported.")
            };
        }

        return new MaterialProperty(rawData, type, value);
    }
}

internal class AnimatedAnimatorProperty : AnimatorProperty
{
    internal AnimatedAnimatorProperty(
        CustomData rawData,
        AnimatorPropertyType animatorPropertyType,
        object value,
        Dictionary<string, List<object>> pointDefinitions)
        : base(rawData, animatorPropertyType, value)
    {
        PointDefinition = rawData.GetPointData<float>(VALUE, pointDefinitions) ??
                          throw new JsonNotDefinedException(VALUE);
    }

    internal PointDefinition<float> PointDefinition { get; }
}

internal class AnimatorProperty
{
    internal AnimatorProperty(CustomData rawData, AnimatorPropertyType animatorPropertyType, object value)
    {
        Name = rawData.GetRequired<string>(ID_FIELD);
        Type = animatorPropertyType;
        Value = value;
    }

    internal string Name { get; }

    internal AnimatorPropertyType Type { get; }

    internal object Value { get; }

    internal static AnimatorProperty CreateAnimatorProperty(
        CustomData rawData,
        Dictionary<string, List<object>> pointDefinitions)
    {
        AnimatorPropertyType type = rawData.GetStringToEnumRequired<AnimatorPropertyType>(TYPE);
        object value;
        if (type == AnimatorPropertyType.Trigger)
        {
            value = rawData.Get<object>(VALUE) ?? true;
        }
        else
        {
            value = rawData.GetRequired<object>(VALUE);
        }

        return value is List<object>
            ? new AnimatedAnimatorProperty(rawData, type, value, pointDefinitions)
            : new AnimatorProperty(rawData, type, value);
    }
}

internal abstract class RenderSettingProperty
{
    internal RenderSettingProperty(string name)
    {
        Name = name;
    }

    internal string Name { get; }

    internal static RenderSettingProperty CreateRenderSettingProperty<T>(
        string name,
        object value,
        CustomData rawData,
        Dictionary<string, List<object>> pointDefinitions)
        where T : struct
    {
        return value is List<object>
            ? new AnimatedRenderSettingProperty<T>(name, rawData, pointDefinitions)
            : new RenderSettingProperty<T>(name, rawData.GetRequired<T>(name));
    }
}

internal class AnimatedRenderSettingProperty<T> : RenderSettingProperty
    where T : struct
{
    internal AnimatedRenderSettingProperty(
        string name,
        CustomData rawData,
        Dictionary<string, List<object>> pointDefinitions)
        : base(name)
    {
        PointDefinition = rawData.GetPointData<T>(name, pointDefinitions) ??
                          throw new JsonNotDefinedException(name);
    }

    internal PointDefinition<T> PointDefinition { get; }
}

internal class RenderSettingProperty<T> : RenderSettingProperty
{
    internal RenderSettingProperty(string name, T value)
        : base(name)
    {
        Value = value;
    }

    internal T Value { get; }
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

    internal int Priority { get; }

    internal List<MaterialProperty>? Properties { get; }

    internal string? Source { get; }

    internal string[]? Target { get; }
}

internal class SetMaterialPropertyData : ICustomEventCustomData
{
    internal SetMaterialPropertyData(CustomData customData, Dictionary<string, List<object>> pointDefinitions)
    {
        Easing = customData.GetStringToEnum<Functions?>(EASING) ?? Functions.easeLinear;
        Duration = customData.Get<float?>(DURATION) ?? 0f;
        Asset = customData.GetRequired<string>(ASSET);
        Properties = customData
            .GetRequired<List<object>>(PROPERTIES)
            .Select(n => MaterialProperty.CreateMaterialProperty((CustomData)n, pointDefinitions))
            .ToList();
    }

    internal string Asset { get; }

    internal float Duration { get; }

    internal Functions Easing { get; }

    internal List<MaterialProperty> Properties { get; }
}

internal class SetGlobalPropertyData : ICustomEventCustomData
{
    internal SetGlobalPropertyData(CustomData customData, Dictionary<string, List<object>> pointDefinitions)
    {
        Easing = customData.GetStringToEnum<Functions?>(EASING) ?? Functions.easeLinear;
        Duration = customData.Get<float?>(DURATION) ?? 0f;
        Properties = customData
            .GetRequired<List<object>>(PROPERTIES)
            .Select(n => MaterialProperty.CreateMaterialProperty((CustomData)n, pointDefinitions))
            .ToList();
    }

    internal float Duration { get; }

    internal Functions Easing { get; }

    internal List<MaterialProperty> Properties { get; }
}

internal class SetCameraPropertyData : ICustomEventCustomData
{
    internal SetCameraPropertyData(CustomData customData)
    {
        List<object>? depthTextureModeStrings = customData.Get<List<object>?>(CAMERA_DEPTH_TEXTURE_MODE);
        if (depthTextureModeStrings != null)
        {
            DepthTextureMode = depthTextureModeStrings.Aggregate(
                UnityEngine.DepthTextureMode.None,
                (current, depthTextureModeString) =>
                    current |
                    (DepthTextureMode)Enum.Parse(typeof(DepthTextureMode), (string)depthTextureModeString));
        }

        ClearFlags = customData.GetStringToEnum<CameraClearFlags?>(CAMERA_CLEAR_FLAGS);
        BackgroundColor = customData.GetColor(CAMERA_BACKGROUND_COLOR);
    }

    internal DepthTextureMode? DepthTextureMode { get; }

    internal CameraClearFlags? ClearFlags { get; }

    internal Color? BackgroundColor { get; }
}

internal class SetAnimatorPropertyData : ICustomEventCustomData
{
    internal SetAnimatorPropertyData(CustomData customData, Dictionary<string, List<object>> pointDefinitions)
    {
        Easing = customData.GetStringToEnum<Functions?>(EASING) ?? Functions.easeLinear;
        Duration = customData.Get<float?>(DURATION) ?? 0f;
        Id = customData.GetRequired<string>(ID_FIELD);
        Properties = customData
            .GetRequired<List<object>>(PROPERTIES)
            .Select(n => AnimatorProperty.CreateAnimatorProperty((CustomData)n, pointDefinitions))
            .ToList();
    }

    internal float Duration { get; }

    internal Functions Easing { get; }

    internal string Id { get; }

    internal List<AnimatorProperty> Properties { get; }
}

internal class SetRenderSettingData : ICustomEventCustomData
{
    internal SetRenderSettingData(
        CustomData customData,
        Dictionary<string, List<object>> pointDefinitions)
    {
        Duration = customData.Get<float?>(DURATION) ?? 0f;
        Easing = customData.GetStringToEnum<Functions?>(EASING) ?? Functions.easeLinear;

        string[] excludedStrings = [DURATION, EASING];
        List<KeyValuePair<string, object?>> propertyKeys =
            customData.Where(n => excludedStrings.All(m => m != n.Key)).ToList();
        foreach ((string? key, object? value) in propertyKeys)
        {
            if (value == null)
            {
                continue;
            }

            switch (key)
            {
                case "ambientIntensity":
                case "ambientMode":
                case "defaultReflectionMode":
                case "defaultReflectionResolution":
                case "flareFadeSpeed":
                case "flareStrength":
                case "fog":
                case "fogDensity":
                case "fogEndDistance":
                case "fogMode":
                case "fogStartDistance":
                case "haloStrength":
                case "reflectionBounces":
                case "reflectionIntensity":
                    Properties.Add(
                        RenderSettingProperty.CreateRenderSettingProperty<float>(
                            key,
                            value,
                            customData,
                            pointDefinitions));
                    break;

                case "ambientEquatorColor":
                case "ambientGroundColor":
                case "ambientLight":
                case "ambientSkyColor":
                case "fogColor":
                case "subtractiveShadowColor":
                    Properties.Add(
                        RenderSettingProperty.CreateRenderSettingProperty<Vector4>(
                            key,
                            value,
                            customData,
                            pointDefinitions));
                    break;

                case "skybox":
                case "sun":
                    Properties.Add(
                        new RenderSettingProperty<string>(key, customData.GetRequired<string>(key)));
                    break;
            }
        }
    }

    internal float Duration { get; }

    internal Functions Easing { get; }

    internal List<RenderSettingProperty> Properties { get; } = [];
}

internal class DeclareCullingMaskData : ICustomEventCustomData
{
    internal DeclareCullingMaskData(CustomData customData, Dictionary<string, Track> tracks)
    {
        Name = customData.GetRequired<string>(ID_FIELD);
        Tracks = customData.GetTrackArray(tracks, false);
        Whitelist = customData.Get<bool?>(WHITELIST) ?? false;
        DepthTexture = customData.Get<bool?>(DEPTH_TEXTURE) ?? false;
    }

    internal bool DepthTexture { get; }

    internal string Name { get; }

    internal IEnumerable<Track> Tracks { get; }

    internal bool Whitelist { get; }
}

internal class DestroyTextureData : ICustomEventCustomData
{
    internal DestroyTextureData(CustomData customData)
    {
        object nameRaw = customData.GetRequired<object>(ID_FIELD);
        Name = nameRaw switch
        {
            string nameString => [nameString],
            List<object> nameArray => nameArray.Select(n => (string)n).ToArray(),
            _ => throw new InvalidCastException(
                $"[{ID_FIELD}] was not an allowable type. Was [{nameRaw.GetType().FullName}].")
        };
    }

    internal string[] Name { get; }
}

internal class DeclareRenderTextureData : ICustomEventCustomData
{
    internal DeclareRenderTextureData(CustomData customData)
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
            Plugin.Log.Warn($"Current graphics card does not support [{Format.Value}].");
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

internal class DestroyPrefabData : ICustomEventCustomData
{
    internal DestroyPrefabData(CustomData customData)
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
        Track = customData.GetNullableTrack(beatmapTracks, false);
    }

    internal string Asset { get; }

    internal string? Id { get; }

    internal Track? Track { get; }

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
