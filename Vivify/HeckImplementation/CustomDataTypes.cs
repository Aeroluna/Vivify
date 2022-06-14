using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
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

    internal readonly struct MaterialProperty
    {
        internal MaterialProperty(CustomData rawData, Dictionary<string, PointDefinition> pointDefinitions)
        {
            Name = rawData.Get<string>(NAME) ?? throw new InvalidOperationException("Property name not found.");
            Type = (MaterialPropertyType)Enum.Parse(
                typeof(MaterialPropertyType),
                rawData.Get<string>(TYPE) ?? throw new InvalidOperationException("Type not found."));
            Value = rawData.Get<object>(VALUE) ?? throw new InvalidOperationException("Property value not found.");
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
            Duration = customData.Get<float?>(DURATION) ?? 0f;
            Pass = customData.Get<int?>(PASS) ?? 0;
            Asset = customData.Get<string>(ASSET) ?? throw new InvalidOperationException("Asset name not found.");
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
            Asset = customData.Get<string>(ASSET) ?? throw new InvalidOperationException("Asset name not found.");
            Properties = customData
                .Get<List<object>>(PROPERTIES)?
                .Select(n => new MaterialProperty((CustomData)n, pointDefinitions))
                .ToList() ?? throw new InvalidOperationException("Properties not found.");
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
            Name = customData.Get<string>(NAME) ?? throw new InvalidOperationException("Mask name not found.");
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
            Name = customData.Get<string>(NAME) ?? throw new InvalidOperationException("Mask name not found.");
            Tracks = customData.GetTrackArray(tracks, false);
        }

        internal string Name { get; }

        internal IEnumerable<Track> Tracks { get; }
    }

    internal class DestroyPrefabData : ICustomEventCustomData
    {
        internal DestroyPrefabData(CustomData customData)
        {
            Id = customData.Get<string>(ID) ?? throw new InvalidOperationException("Id not found.");
        }

        internal string Id { get; }
    }

    internal class InstantiatePrefabData : ICustomEventCustomData
    {
        internal InstantiatePrefabData(CustomData customData)
        {
            Asset = customData.Get<string>(ASSET) ?? throw new InvalidOperationException("Asset name not found.");
            Id = customData.Get<string>(ID);
            Position = customData.GetVector3(POSITION);
            Rotation = customData.GetVector3(ROTATION);
            Scale = customData.GetVector3(SCALE);
        }

        internal string Asset { get; }

        internal Vector3? Position { get; }

        internal Vector3? Rotation { get; }

        internal Vector3? Scale { get; }

        internal string? Id { get; }
    }
}
