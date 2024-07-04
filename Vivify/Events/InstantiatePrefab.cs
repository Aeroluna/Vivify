using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation.Transform;
using Heck.Deserialize;
using Heck.Event;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Controllers.Sync;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events
{
    [CustomEvent(INSTANTIATE_PREFAB)]
    internal class InstantiatePrefab : ICustomEvent
    {
        private readonly SiraLog _log;
        private readonly IInstantiator _instantiator;
        private readonly AssetBundleManager _assetBundleManager;
        private readonly PrefabManager _prefabManager;
        private readonly DeserializedData _deserializedData;
        private readonly TransformControllerFactory _transformControllerFactory;
        private readonly bool _leftHanded;

        private InstantiatePrefab(
            SiraLog log,
            IInstantiator instantiator,
            AssetBundleManager assetBundleManager,
            PrefabManager prefabManager,
            [Inject(Id = ID)] DeserializedData deserializedData,
            TransformControllerFactory transformControllerFactory,
            [Inject(Id = HeckController.LEFT_HANDED_ID)] bool leftHanded)
        {
            _log = log;
            _instantiator = instantiator;
            _assetBundleManager = assetBundleManager;
            _prefabManager = prefabManager;
            _deserializedData = deserializedData;
            _transformControllerFactory = transformControllerFactory;
            _leftHanded = leftHanded;
        }

        public void Callback(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out InstantiatePrefabData? data))
            {
                return;
            }

            string assetName = data.Asset;
            if (!_assetBundleManager.TryGetAsset(assetName, out GameObject? prefab))
            {
                return;
            }

            GameObject gameObject = Object.Instantiate(prefab);

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
                _log.Debug($"Created [{assetName}] with id [{id}]");
                _prefabManager.Add(id, gameObject, data.Track);
            }
            else
            {
                string genericId = gameObject.GetHashCode().ToString();
                _log.Debug($"Created [{assetName}] without id");
                _prefabManager.Add(genericId, gameObject, data.Track);
            }
        }
    }
}
