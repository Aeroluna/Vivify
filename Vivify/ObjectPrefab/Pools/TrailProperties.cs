using System;
using UnityEngine;

namespace Vivify.ObjectPrefab.Pools;

internal readonly struct TrailProperties : IEquatable<TrailProperties>
{
    internal TrailProperties(
        Vector3? topPos,
        Vector3? bottomPos,
        float? duration,
        int? samplingFrequency,
        int? granularity)
    {
        TopPos = topPos;
        BottomPos = bottomPos;
        Duration = duration;
        SamplingFrequency = samplingFrequency;
        Granularity = granularity;
    }

    internal Vector3? TopPos { get; }

    internal Vector3? BottomPos { get; }

    internal float? Duration { get; }

    internal int? SamplingFrequency { get; }

    internal int? Granularity { get; }

    public static bool operator ==(TrailProperties lhs, TrailProperties rhs)
    {
        return lhs.Equals(rhs);
    }

    public static bool operator !=(TrailProperties lhs, TrailProperties rhs)
    {
        return !lhs.Equals(rhs);
    }

    public override bool Equals(object? obj)
    {
        if (obj is TrailProperties other)
        {
            return Equals(other);
        }

        return false;
    }

    public bool Equals(TrailProperties other)
    {
        return Nullable.Equals(TopPos, other.TopPos) &&
               Nullable.Equals(BottomPos, other.BottomPos) &&
               Nullable.Equals(Duration, other.Duration) &&
               SamplingFrequency == other.SamplingFrequency &&
               Granularity == other.Granularity;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = TopPos.GetHashCode();
            hashCode = (hashCode * 397) ^ BottomPos.GetHashCode();
            hashCode = (hashCode * 397) ^ Duration.GetHashCode();
            hashCode = (hashCode * 397) ^ SamplingFrequency.GetHashCode();
            hashCode = (hashCode * 397) ^ Granularity.GetHashCode();
            return hashCode;
        }
    }
}
