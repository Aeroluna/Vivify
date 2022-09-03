using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
using Heck.Animation.Transform;
using UnityEngine;
using static Heck.HeckController;
using static Vivify.VivifyController;

namespace Vivify
{
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
            PointDefinition = rawData.GetPointData<T>(VALUE, pointDefinitions) ?? throw new JsonNotDefinedException(VALUE);
        }

        internal PointDefinition<T> PointDefinition { get; }
    }

    internal class MaterialProperty
    {
        internal MaterialProperty(CustomData rawData, MaterialPropertyType materialPropertyType, object value)
        {
            Name = Shader.PropertyToID(rawData.GetRequired<string>(NAME));
            Type = materialPropertyType;
            Value = value;
        }

        internal int Name { get; }

        internal MaterialPropertyType Type { get; }

        internal object Value { get; }

        internal static MaterialProperty CreateMaterialProperty(CustomData rawData, Dictionary<string, List<object>> pointDefinitions)
        {
            MaterialPropertyType type = rawData.GetStringToEnumRequired<MaterialPropertyType>(TYPE);
            object value = rawData.GetRequired<object>(VALUE);
            if (value is List<object>)
            {
                return type switch
                {
                    MaterialPropertyType.Color => new AnimatedMaterialProperty<Vector4>(rawData, type, value, pointDefinitions),
                    MaterialPropertyType.Float => new AnimatedMaterialProperty<float>(rawData, type, value, pointDefinitions),
                    _ => throw new InvalidOperationException()
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
            PointDefinition = rawData.GetPointData<float>(VALUE, pointDefinitions) ?? throw new JsonNotDefinedException(VALUE);
        }

        internal PointDefinition<float> PointDefinition { get; }
    }

    internal class AnimatorProperty
    {
        internal AnimatorProperty(CustomData rawData, AnimatorPropertyType animatorPropertyType, object value)
        {
            Name = rawData.GetRequired<string>(NAME);
            Type = animatorPropertyType;
            Value = value;
        }

        internal string Name { get; }

        internal AnimatorPropertyType Type { get; }

        internal object Value { get; }

        internal static AnimatorProperty CreateAnimatorProperty(CustomData rawData, Dictionary<string, List<object>> pointDefinitions)
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

    internal class ApplyPostProcessingData : ICustomEventCustomData
    {
        internal ApplyPostProcessingData(CustomData customData, Dictionary<string, List<object>> pointDefinitions)
        {
            Easing = customData.GetStringToEnum<Functions?>(EASING) ?? Functions.easeLinear;
            Duration = customData.GetRequired<float>(DURATION);
            Priority = customData.Get<int?>(PRIORITY) ?? 0;
            Target = customData.Get<List<object>?>(TARGET)?.Cast<string>().ToArray();
            Asset = customData.Get<string?>(ASSET);
            Pass = customData.Get<int?>(PASS);
            List<object>? properties = customData.Get<List<object>>(PROPERTIES);
            if (properties != null)
            {
                Properties = properties
                    .Select(n => MaterialProperty.CreateMaterialProperty((CustomData)n, pointDefinitions))
                    .ToList();
            }
        }

        internal Functions Easing { get; }

        internal float Duration { get; }

        internal int Priority { get; }

        internal string[]? Target { get; }

        internal string? Asset { get; }

        internal int? Pass { get; }

        internal List<MaterialProperty>? Properties { get; }
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

        internal Functions Easing { get; }

        internal float Duration { get; }

        internal string Asset { get; }

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

        internal Functions Easing { get; }

        internal float Duration { get; }

        internal List<MaterialProperty> Properties { get; }
    }

    internal class SetAnimatorPropertyData : ICustomEventCustomData
    {
        internal SetAnimatorPropertyData(CustomData customData, Dictionary<string, List<object>> pointDefinitions)
        {
            Easing = customData.GetStringToEnum<Functions?>(EASING) ?? Functions.easeLinear;
            Duration = customData.Get<float?>(DURATION) ?? 0f;
            Id = customData.GetRequired<string>(PREFAB_ID);
            Properties = customData
                .GetRequired<List<object>>(PROPERTIES)
                .Select(n => AnimatorProperty.CreateAnimatorProperty((CustomData)n, pointDefinitions))
                .ToList();
        }

        internal Functions Easing { get; }

        internal float Duration { get; }

        internal string Id { get; }

        internal List<AnimatorProperty> Properties { get; }
    }

    internal class DeclareCullingMaskData : ICustomEventCustomData
    {
        internal DeclareCullingMaskData(CustomData customData, Dictionary<string, Track> tracks)
        {
            Name = customData.GetRequired<string>(NAME);
            Tracks = customData.GetTrackArray(tracks, false);
            Whitelist = customData.Get<bool?>(WHITELIST) ?? false;
            DepthTexture = customData.Get<bool?>(DEPTH_TEXTURE) ?? false;
        }

        internal string Name { get; }

        internal IEnumerable<Track> Tracks { get; }

        internal bool Whitelist { get; }

        internal bool DepthTexture { get; }
    }

    internal class DeclareRenderTextureData : ICustomEventCustomData
    {
        internal DeclareRenderTextureData(CustomData customData)
        {
            Name = customData.GetRequired<string>(NAME);
            XRatio = customData.Get<float?>(XRATIO) ?? 1;
            YRatio = customData.Get<float?>(YRATIO) ?? 1;
            Width = customData.Get<int?>(WIDTH);
            Height = customData.Get<int?>(HEIGHT);
            PropertyId = Shader.PropertyToID(Name);
        }

        internal int PropertyId { get; }

        internal string Name { get; }

        internal float XRatio { get; }

        internal float YRatio { get; }

        internal int? Width { get; }

        internal int? Height { get; }
    }

    internal class DestroyPrefabData : ICustomEventCustomData
    {
        internal DestroyPrefabData(CustomData customData)
        {
            Id = customData.GetRequired<string>(PREFAB_ID);
        }

        internal string Id { get; }
    }

    internal class InstantiatePrefabData : ICustomEventCustomData
    {
        internal InstantiatePrefabData(
            CustomData customData,
            Dictionary<string, Track> beatmapTracks)
        {
            Asset = customData.GetRequired<string>(ASSET);
            Id = customData.Get<string>(PREFAB_ID);
            TransformData = new TransformData(customData);
            Track = customData.GetNullableTrack(beatmapTracks, false);
        }

        internal string Asset { get; }

        internal TransformData TransformData { get; }

        internal string? Id { get; }

        internal Track? Track { get; }
    }
}
