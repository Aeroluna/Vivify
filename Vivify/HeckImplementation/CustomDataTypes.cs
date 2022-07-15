﻿using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
using Heck.Animation.Transform;
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

    internal readonly struct MaterialProperty
    {
        internal MaterialProperty(CustomData rawData, Dictionary<string, PointDefinition> pointDefinitions)
        {
            Name = rawData.GetRequired<string>(NAME);
            Type = (MaterialPropertyType)Enum.Parse(
                typeof(MaterialPropertyType),
                rawData.GetRequired<string>(TYPE));
            Value = rawData.GetRequired<object>(VALUE);
            PointDefinition = Value is List<object> ? rawData.GetPointData(VALUE, pointDefinitions) : null;
        }

        internal string Name { get; }

        internal MaterialPropertyType Type { get; }

        internal object Value { get; }

        internal PointDefinition? PointDefinition { get; }
    }

    internal class ApplyPostProcessingData : ICustomEventCustomData
    {
        internal ApplyPostProcessingData(CustomData customData, Dictionary<string, PointDefinition> pointDefinitions)
        {
            Easing = customData.GetStringToEnum<Functions?>(EASING) ?? Functions.easeLinear;
            Duration = customData.GetRequired<float>(DURATION);
            Pass = customData.Get<int?>(PASS) ?? 0;
            Asset = customData.GetRequired<string>(ASSET);
            List<object>? properties = customData.Get<List<object>>(PROPERTIES);
            if (properties != null)
            {
                Properties = properties
                    .Select(n => new MaterialProperty((CustomData)n, pointDefinitions))
                    .ToList();
            }
        }

        internal Functions Easing { get; }

        internal float Duration { get; }

        internal int Pass { get; }

        internal string Asset { get; }

        internal List<MaterialProperty>? Properties { get; }
    }

    internal class SetMaterialPropertyData : ICustomEventCustomData
    {
        internal SetMaterialPropertyData(CustomData customData, Dictionary<string, PointDefinition> pointDefinitions)
        {
            Easing = customData.GetStringToEnum<Functions?>(EASING) ?? Functions.easeLinear;
            Duration = customData.Get<float?>(DURATION) ?? 0f;
            Asset = customData.GetRequired<string>(ASSET);
            Properties = customData
                .GetRequired<List<object>>(PROPERTIES)
                .Select(n => new MaterialProperty((CustomData)n, pointDefinitions))
                .ToList();
        }

        internal Functions Easing { get; }

        internal float Duration { get; }

        internal string Asset { get; }

        internal List<MaterialProperty> Properties { get; }
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

    internal class DeclareMaskData : ICustomEventCustomData
    {
        internal DeclareMaskData(CustomData customData, Dictionary<string, Track> tracks)
        {
            Name = customData.GetRequired<string>(NAME);
            Tracks = customData.GetTrackArray(tracks, false);
        }

        internal string Name { get; }

        internal IEnumerable<Track> Tracks { get; }
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
        internal InstantiatePrefabData(CustomData customData)
        {
            Asset = customData.GetRequired<string>(ASSET);
            Id = customData.Get<string>(PREFAB_ID);
            TransformData = new TransformData(customData);
        }

        internal string Asset { get; }

        internal TransformData TransformData { get; }

        internal string? Id { get; }
    }
}
