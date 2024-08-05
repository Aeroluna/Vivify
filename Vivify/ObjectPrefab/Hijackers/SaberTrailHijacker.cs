using System.Collections.Generic;
using UnityEngine;

namespace Vivify.ObjectPrefab.Hijackers;

internal class SaberTrailHijacker : IHijacker<FollowedSaberTrail>
{
    private readonly SaberTrail _saberTrail;

    internal SaberTrailHijacker(SaberTrail saberTrail)
    {
        _saberTrail = saberTrail;
    }

    public void Activate(List<FollowedSaberTrail> followedSaberTrails, bool hideOriginal)
    {
        Transform parent = _saberTrail.transform.parent;
        foreach (FollowedSaberTrail followedSaberTrail in followedSaberTrails)
        {
            followedSaberTrail.Init(_saberTrail, parent);
        }

        if (hideOriginal)
        {
            _saberTrail._trailRenderer._meshRenderer.enabled = false;
        }
    }

    public void Deactivate()
    {
        _saberTrail._trailRenderer._meshRenderer.enabled = true;
    }
}
