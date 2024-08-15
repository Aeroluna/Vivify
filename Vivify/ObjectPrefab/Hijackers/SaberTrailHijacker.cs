using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using IPA.Loader;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;

namespace Vivify.ObjectPrefab.Hijackers;

internal class SaberTrailHijacker : IHijacker<FollowedSaberTrail>
{
    private static Type? _sfTrailType;
    private static FieldInfo? _vertexPoolField;
    private static FieldInfo? _vertexPoolMeshRendererField;
    private static Type? _vertexPoolType;

    private readonly SiraLog _log;

    private readonly HashSet<Renderer> _originalRenderers = [];
    private readonly Transform _parent;
    private readonly SaberTrail _saberTrail;

    private bool _shouldHide;

    [UsedImplicitly]
    internal SaberTrailHijacker(SiraLog log, SaberTrail saberTrail, ColorManager colorManager)
    {
        _log = log;
        _saberTrail = saberTrail;
        _parent = saberTrail.transform.parent;

        if (!saberTrail.enabled)
        {
            // indicates that sirautil created an incomplete trail, lets fix it up so we can follow it
            Saber saber = saberTrail.GetComponentInParent<Saber>();
            Color color = colorManager.ColorForSaberType(saber.saberType);
            saberTrail.Setup(color, saber._movementData);
            saberTrail.enabled = true;
            saberTrail._trailRenderer._meshRenderer.enabled = false;
        }
        else
        {
            _originalRenderers.Add(saberTrail._trailRenderer._meshRenderer);
        }

        CheckSaberFactory();
        CheckCustomSabersLite();
        return;

        void CheckSaberFactory()
        {
            Assembly? saberFactory = PluginManager.GetPlugin("Saber Factory")?.Assembly;
            if (saberFactory == null)
            {
                return;
            }

            _sfTrailType ??= saberFactory.GetType("SaberFactory.Instances.Trail.SFTrail");
            _vertexPoolField ??= _sfTrailType?.GetField("_vertexPool", AccessTools.all);
            _vertexPoolType ??= saberFactory.GetType("SaberFactory.Misc.VertexPool");
            _vertexPoolMeshRendererField ??= _vertexPoolType.GetField("MeshRenderer", AccessTools.all);
            object? sfTrail = saberTrail.GetComponentInChildren(_sfTrailType);
            if (sfTrail != null)
            {
                object? vertexPool = _vertexPoolField?.GetValue(sfTrail);
                if (vertexPool != null)
                {
                    object? vertexPoolMeshRenderer = _vertexPoolMeshRendererField?.GetValue(vertexPool);
                    if (vertexPoolMeshRenderer is MeshRenderer meshRenderer)
                    {
                        _originalRenderers.Add(meshRenderer);
                        return;
                    }
                }
            }

            log.Error("Could not fetch Saber Factory trail");
        }

        void CheckCustomSabersLite()
        {
            if (PluginManager.GetPlugin("CustomSabersLite") == null)
            {
                return;
            }

            SaberTrail[] saberTrails = saberTrail
                .GetComponentsInChildren<SaberTrail>()
                .Where(n => n != saberTrail)
                .ToArray();

            if (saberTrails.Length == 0)
            {
                log.Warn("CustomSabersLite trail not yet created, deferring renderer fetching");
                saberTrail.gameObject.AddComponent<LiteTrailContract>().Init(this);
            }
            else
            {
                _originalRenderers.UnionWith(saberTrails.Select(n => n._trailRenderer._meshRenderer));
            }
        }
    }

    public void Activate(List<FollowedSaberTrail> followedSaberTrails, bool hideOriginal)
    {
        foreach (FollowedSaberTrail followedSaberTrail in followedSaberTrails)
        {
            followedSaberTrail.transform.SetParent(_parent, false);
            followedSaberTrail.Init(_saberTrail, _parent);
        }

        // ReSharper disable once InvertIf
        if (hideOriginal)
        {
            _shouldHide = true;
            foreach (Renderer originalRenderer in _originalRenderers)
            {
                originalRenderer.enabled = false;
            }
        }
    }

    public void Deactivate()
    {
        _shouldHide = false;
        foreach (Renderer originalRenderer in _originalRenderers)
        {
            originalRenderer.enabled = true;
        }
    }

    private class LiteTrailContract : MonoBehaviour
    {
        private SaberTrailHijacker _hijacker = null!;

        internal void Init(SaberTrailHijacker hijacker)
        {
            _hijacker = hijacker;
        }

        private void OnTransformChildrenChanged()
        {
            SaberTrail saberTrail = _hijacker._saberTrail;
            SaberTrail[] saberTrails = saberTrail
                .GetComponentsInChildren<SaberTrail>()
                .Where(n => n != saberTrail)
                .ToArray();
            HashSet<Renderer> renderers = _hijacker._originalRenderers;
            MeshRenderer[] newRenderers = saberTrails.Select(n => n._trailRenderer._meshRenderer).ToArray();
            renderers.UnionWith(newRenderers);

            if (_hijacker._shouldHide)
            {
                foreach (MeshRenderer meshRenderer in newRenderers)
                {
                    meshRenderer.enabled = false;
                }
            }

            _hijacker._log.Info("Fetched CustomSabersLite trail");
            Destroy(this);
        }
    }
}
