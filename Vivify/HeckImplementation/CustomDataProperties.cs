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
