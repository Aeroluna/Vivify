using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using Heck.Deserialize;
using UnityEngine;
using static Heck.HeckController;
using static Vivify.VivifyController;

namespace Vivify;

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

internal class CameraProperty
{
    internal CameraProperty(
        bool hasDepthTextureMode,
        bool hasClearFlags,
        bool hasBackgroundColor,
        bool hasCulling,
        bool hasBloomPrePass,
        bool hasMainEffect,
        DepthTextureMode? depthTextureMode,
        CameraClearFlags? clearFlags,
        Color? backgroundColor,
        CullingData? culling,
        bool? bloomPrePass,
        bool? mainEffect)
    {
        HasDepthTextureMode = hasDepthTextureMode;
        HasClearFlags = hasClearFlags;
        HasBackgroundColor = hasBackgroundColor;
        HasCulling = hasCulling;
        HasBloomPrePass = hasBloomPrePass;
        HasMainEffect = hasMainEffect;
        DepthTextureMode = depthTextureMode;
        ClearFlags = clearFlags;
        BackgroundColor = backgroundColor;
        Culling = culling;
        BloomPrePass = bloomPrePass;
        MainEffect = mainEffect;
    }

    internal bool HasDepthTextureMode { get; }

    internal bool HasClearFlags { get; }

    internal bool HasBackgroundColor { get; }

    internal bool HasCulling { get; }

    internal bool HasBloomPrePass { get; }

    internal bool HasMainEffect { get; }

    internal DepthTextureMode? DepthTextureMode { get; }

    internal CameraClearFlags? ClearFlags { get; }

    internal Color? BackgroundColor { get; }

    internal CullingData? Culling { get; }

    internal bool? BloomPrePass { get; }

    internal bool? MainEffect { get; }

    internal static CameraProperty CreateCameraProperty(CustomData customData, Dictionary<string, Track> tracks)
    {
        bool hasDepthTextureMode = customData.TryGetValue(CAMERA_DEPTH_TEXTURE_MODE, out object? depthTextureModeStrings);
        DepthTextureMode? depthTextureMode = null;
        if (hasDepthTextureMode &&
            depthTextureModeStrings != null)
        {
            depthTextureMode = ((List<object>)depthTextureModeStrings).Aggregate(
                UnityEngine.DepthTextureMode.None,
                (current, depthTextureModeString) =>
                    current |
                    (DepthTextureMode)Enum.Parse(typeof(DepthTextureMode), (string)depthTextureModeString));
        }

        bool hasClearFlags = customData.TryGetValue(CAMERA_CLEAR_FLAGS, out object? clearFlagsString);
        CameraClearFlags? clearFlags = hasClearFlags && clearFlagsString != null
            ? (CameraClearFlags)Enum.Parse(typeof(CameraClearFlags), (string)clearFlagsString)
            : null;

        bool hasBackgroundColor = customData.TryGetValue(CAMERA_BACKGROUND_COLOR, out object? backgroundColorString);
        Color? backgroundColor = null;
        if (hasBackgroundColor &&
            backgroundColorString != null)
        {
            List<float> color = ((List<object>)backgroundColorString).Select(Convert.ToSingle).ToList();
            backgroundColor = new Color(color[0], color[1], color[2], color.Count > 3 ? color[3] : 1);
        }

        bool hasCulling = customData.TryGetValue(CULLING, out object? cullingData);
        CullingData? culling = hasCulling && cullingData != null ? new CullingData((CustomData)cullingData, tracks) : null;

        bool hasBloomPrePass = customData.TryGetValue(BLOOMPREPASS, out object? bloomPrePassString);
        bool? bloomPrePass = hasBloomPrePass && bloomPrePassString != null
            ? (bool)bloomPrePassString
            : null;

        bool hasMainEffect = customData.TryGetValue(MAIN_EFFECT, out object? mainEffectString);
        bool? mainEffect = hasMainEffect && mainEffectString != null
            ? (bool)mainEffectString
            : null;

        return new CameraProperty(
            hasDepthTextureMode,
            hasClearFlags,
            hasBackgroundColor,
            hasCulling,
            hasBloomPrePass,
            hasMainEffect,
            depthTextureMode,
            clearFlags,
            backgroundColor,
            culling,
            bloomPrePass,
            mainEffect);
    }

    internal class CullingData
    {
        internal CullingData(CustomData customData, Dictionary<string, Track> tracks)
        {
            Tracks = customData.GetTrackArray(tracks, false);
            Whitelist = customData.Get<bool?>(WHITELIST) ?? false;
        }

        internal IEnumerable<Track> Tracks { get; }

        internal bool Whitelist { get; }
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

internal abstract class RenderingSettingsProperty
{
    internal RenderingSettingsProperty(string name)
    {
        Name = name;
    }

    internal string Name { get; }

    internal static RenderingSettingsProperty CreateRenderSettingProperty<T>(
        string name,
        object value,
        CustomData rawData,
        Dictionary<string, List<object>> pointDefinitions)
        where T : struct
    {
        return value is List<object>
            ? new AnimatedRenderingSettingsProperty<T>(name, rawData, pointDefinitions)
            : new RenderingSettingsProperty<T>(name, rawData.GetRequired<T>(name));
    }
}

internal class AnimatedRenderingSettingsProperty<T> : RenderingSettingsProperty
    where T : struct
{
    internal AnimatedRenderingSettingsProperty(
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

internal class RenderingSettingsProperty<T> : RenderingSettingsProperty
{
    internal RenderingSettingsProperty(string name, T value)
        : base(name)
    {
        Value = value;
    }

    internal T Value { get; }
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
    internal SetCameraPropertyData(CustomData customData, Dictionary<string, Track> tracks)
    {
        Id = customData.Get<string?>(ID_FIELD) ?? CAMERA_TARGET;
        Property = CameraProperty.CreateCameraProperty(customData.GetRequired<CustomData>(PROPERTIES), tracks);
    }

    internal string Id { get; }

    internal CameraProperty Property { get; }
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
