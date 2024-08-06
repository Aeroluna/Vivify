using System;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck;
using Heck.Animation.Transform;
using Heck.Deserialize;
using Heck.Event;
using Heck.ReLoad;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Controllers.Sync;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;
using Object = UnityEngine.Object;

namespace Vivify.Events;

[CustomEvent(INSTANTIATE_PREFAB)]
internal class InstantiatePrefab : ICustomEvent, IInitializable, IDisposable
{
    private readonly AssetBundleManager _assetBundleManager;
    private readonly DeserializedData _deserializedData;
    private readonly IInstantiator _instantiator;
    private readonly bool _leftHanded;
    private readonly ReLoader? _reLoader;
    private readonly SiraLog _log;
    private readonly PrefabManager _prefabManager;
    private readonly IReadonlyBeatmapData _readonlyBeatmapData;
    private readonly TransformControllerFactory _transformControllerFactory;

    private readonly Dictionary<InstantiatePrefabData, GameObject> _loadedPrefabs = new();

    private InstantiatePrefab(
        SiraLog log,
        IInstantiator instantiator,
        AssetBundleManager assetBundleManager,
        PrefabManager prefabManager,
        IReadonlyBeatmapData readonlyBeatmapData,
        [Inject(Id = ID)] DeserializedData deserializedData,
        TransformControllerFactory transformControllerFactory,
        [Inject(Id = HeckController.LEFT_HANDED_ID)] bool leftHanded,
        [InjectOptional] ReLoader? reLoader)
    {
        _log = log;
        _instantiator = instantiator;
        _assetBundleManager = assetBundleManager;
        _prefabManager = prefabManager;
        _readonlyBeatmapData = readonlyBeatmapData;
        _deserializedData = deserializedData;
        _transformControllerFactory = transformControllerFactory;
        _leftHanded = leftHanded;
        _reLoader = reLoader;
        if (reLoader != null)
        {
            reLoader.Rewinded += OnRewind;
        }
    }

    public void Dispose()
    {
        if (_reLoader != null)
        {
            _reLoader.Rewinded -= OnRewind;
        }

        DestroyAllPrefabs();
    }

    public void Initialize()
    {
        if (_readonlyBeatmapData is not CustomBeatmapData customBeatmapData)
        {
            return;
        }

        foreach (CustomEventData customEventData in customBeatmapData.customEventDatas)
        {
            if (customEventData.eventType != INSTANTIATE_PREFAB ||
                !_deserializedData.Resolve(customEventData, out InstantiatePrefabData? data))
            {
                continue;
            }

            string assetName = data.Asset;
            if (!_assetBundleManager.TryGetAsset(assetName, out GameObject? prefab))
            {
                continue;
            }

            GameObject gameObject = Object.Instantiate(prefab);
            gameObject.SetActive(false);
            _loadedPrefabs.Add(data, gameObject);
        }
    }

    public void Callback(CustomEventData customEventData)
    {
        if (!_deserializedData.Resolve(customEventData, out InstantiatePrefabData? data))
        {
            return;
        }

        if (!_loadedPrefabs.TryGetValue(data, out GameObject gameObject))
        {
            return;
        }

        gameObject.SetActive(true);
        Transform transform = gameObject.transform;
        data.TransformData.Apply(transform, false);
        if (_leftHanded)
        {
            transform.localPosition = transform.localPosition.Mirror();
            transform.localRotation = transform.localRotation.Mirror();
            transform.localScale = transform.localScale.Mirror();
        }

        if (data.Track != null)
        {
            data.Track.AddGameObject(gameObject);
            _transformControllerFactory.Create(gameObject, data.Track);
        }

        _instantiator.SongSynchronize(gameObject, customEventData.time);

        string? id = data.Id;
        if (id != null)
        {
            _log.Debug($"Enabled [{data.Asset}] with id [{id}]");
            _prefabManager.Add(id, gameObject, data.Track);
        }
        else
        {
            string genericId = gameObject.GetHashCode().ToString();
            _log.Debug($"Enabled [{data.Asset}] without id");
            _prefabManager.Add(genericId, gameObject, data.Track);
        }
    }

    private void OnRewind()
    {
        DestroyAllPrefabs();
        Initialize();
    }

    private void DestroyAllPrefabs()
    {
        _loadedPrefabs.Values.Do(Object.Destroy);
        _loadedPrefabs.Clear();
    }
}
