using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Heck.Animation;
using Heck.ReLoad;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Managers;
using Vivify.ObjectPrefab.Collections;
using Vivify.ObjectPrefab.Hijackers;
using Vivify.ObjectPrefab.Pools;
using Zenject;

namespace Vivify.ObjectPrefab.Managers
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal enum LoadMode
    {
        Single,
        Additive
    }

    internal class BeatmapObjectPrefabManager : IDisposable
    {
        private readonly AssetBundleManager _assetBundleManager;
        private readonly IInstantiator _instantiator;
        private readonly SiraLog _log;

        private readonly Dictionary<string, IPrefabPool> _prefabPools = new();
        private readonly ReLoader? _reLoader;
        private readonly Dictionary<(string, TrailProperties), TrailPool> _trailPools = new();

        [UsedImplicitly]
        private BeatmapObjectPrefabManager(
            SiraLog log,
            AssetBundleManager assetBundleManager,
            IInstantiator instantiator,
            [InjectOptional] ReLoader? reLoader)
        {
            _log = log;
            _assetBundleManager = assetBundleManager;
            _instantiator = instantiator;
            _reLoader = reLoader;
            if (reLoader != null)
            {
                reLoader.Rewinded += OnRewind;
            }
        }

        internal Dictionary<Component, HashSet<IPrefabPool?>> ActivePools { get; } = new();

        internal Dictionary<Component, IHijacker> Hijackers { get; } = new();

        public void Dispose()
        {
            if (_reLoader != null)
            {
                _reLoader.Rewinded -= OnRewind;
            }

            _prefabPools.Values.Do(n => n.Dispose());
        }

        internal void AssignGameObjectPrefab(
            PrefabList prefabList,
            string? assetName,
            LoadMode loadMode,
            float time)
        {
            PrefabPool? prefabPool = GetGameObjectPrefabPool(assetName);
            if (!prefabList.AddPool(prefabPool, loadMode, time))
            {
                _log.Error($"Could not assign [{assetName}], already assigned");
            }
        }

        internal void AssignTrackPrefab(
            PrefabDictionary prefabDictionary,
            IReadOnlyList<Track> tracks,
            string? assetName,
            LoadMode loadMode)
        {
            PrefabPool? prefabPool = GetGameObjectPrefabPool(assetName);
            foreach (Track track in tracks)
            {
                if (!prefabDictionary.AddPrefabPool(track, prefabPool, loadMode))
                {
                    _log.Error($"Could not assign [{assetName}], is already on track");
                }
            }
        }

        internal void AssignTrail(
            TrailList trailList,
            string? assetName,
            TrailProperties trailProperties,
            LoadMode loadMode,
            float time)
        {
            TrailPool? trailPool = GetTrailPool(assetName, trailProperties);
            if (!trailList.AddPool(trailPool, loadMode, time))
            {
                _log.Error($"Could not assign [{assetName}], already assigned");
            }
        }

        internal void Despawn(Component component)
        {
            if (!ActivePools.TryGetValue(component, out HashSet<IPrefabPool?> pools))
            {
                return;
            }

            if (Hijackers.TryGetValue(component, out IHijacker hijacker))
            {
                hijacker.Deactivate();
            }

            foreach (IPrefabPool? n in pools)
            {
                n?.Despawn(component);
            }

            ActivePools.Remove(component);
        }

        internal void Spawn(
            IEnumerable<Track> tracks,
            PrefabDictionary prefabDictionary,
            Component component,
            float startTime)
        {
            IHijacker? hijacker = null;
            HashSet<IPrefabPool?>? activePool = null;
            List<GameObject>? spawned = null;
            bool hideOriginal = false;
            foreach (Track track in tracks)
            {
                if (!prefabDictionary.TryGetValue(track, out HashSet<PrefabPool?> prefabPools))
                {
                    continue;
                }

                Spawn(
                    prefabPools,
                    component,
                    startTime,
                    ref hijacker,
                    ref activePool,
                    ref spawned,
                    ref hideOriginal);
            }

            if (spawned != null)
            {
                ((IHijacker<GameObject>?)hijacker)?.Activate(spawned, hideOriginal);
            }
        }

        internal void Spawn<TPool, TSpawned>(
            PrefabList<TPool> prefabList,
            Component component,
            float startTime)
            where TPool : class, IPrefabPool<TSpawned>
        {
            IHijacker? hijacker = null;
            HashSet<IPrefabPool?>? activePool = null;
            List<TSpawned>? spawned = null;
            bool hideOriginal = false;
            Spawn(
                prefabList.HashSet,
                component,
                startTime,
                ref hijacker,
                ref activePool,
                ref spawned,
                ref hideOriginal);

            if (spawned != null)
            {
                ((IHijacker<TSpawned>?)hijacker)?.Activate(spawned, hideOriginal);
            }
        }

        private PrefabPool? GetGameObjectPrefabPool(string? assetName)
        {
            if (assetName == null)
            {
                return null;
            }

            // ReSharper disable once InvertIf
            if (!_prefabPools.TryGetValue(assetName, out IPrefabPool? prefabPool))
            {
                if (!_assetBundleManager.TryGetAsset(assetName, out GameObject? prefab))
                {
                    return null;
                }

                _prefabPools[assetName] = prefabPool = new PrefabPool(prefab, _instantiator);
            }

            return (PrefabPool)prefabPool;
        }

        private TrailPool? GetTrailPool(string? assetName, TrailProperties trailProperties)
        {
            if (assetName == null)
            {
                return null;
            }

            // ReSharper disable once InvertIf
            if (!_trailPools.TryGetValue((assetName, trailProperties), out TrailPool? trailPool))
            {
                if (!_assetBundleManager.TryGetAsset(assetName, out Material? material))
                {
                    return null;
                }

                _trailPools[(assetName, trailProperties)] = trailPool = new TrailPool(material, trailProperties);
            }

            return trailPool;
        }

        private void OnRewind()
        {
            foreach (Component activeComponent in ActivePools.Keys.ToArray())
            {
                Despawn(activeComponent);
            }
        }

        private void Spawn<TPool, TSpawned>(
            HashSet<TPool?> prefabPools,
            Component component,
            float startTime,
            ref IHijacker? hijacker,
            ref HashSet<IPrefabPool?>? activePool,
            ref List<TSpawned>? spawned,
            ref bool hideOriginal)
            where TPool : IPrefabPool<TSpawned>
        {
            if (prefabPools.Count == 0)
            {
                return;
            }

            if (hijacker == null &&
                !Hijackers.TryGetValue(component, out hijacker))
            {
                Hijackers[component] = hijacker = component switch
                {
                    SaberTrail saberTrail => new SaberTrailHijacker(saberTrail),
                    SaberModelController saberModelController => new SaberModelControllerHijacker(
                        saberModelController,
                        _instantiator),
                    _ => new MpbControllerHijacker(component)
                };
            }

            if (activePool == null &&
                !ActivePools.TryGetValue(component, out activePool))
            {
                ActivePools[component] = activePool = new HashSet<IPrefabPool?>();
            }

            spawned ??= new List<TSpawned>(prefabPools.Count);
            bool hasNull = false;
            foreach (TPool? prefabPool in prefabPools)
            {
                if (prefabPool == null)
                {
                    hasNull = true;
                }
                else
                {
                    spawned.Add(prefabPool.Spawn(component, startTime));
                    activePool.Add(prefabPool);
                }
            }

            if (!hasNull)
            {
                hideOriginal = true;
            }
        }
    }
}
