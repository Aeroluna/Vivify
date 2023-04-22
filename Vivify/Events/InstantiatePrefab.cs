using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck;
using UnityEngine;
using UnityEngine.Video;
using Vivify.Controllers.Sync;
using Object = UnityEngine.Object;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void InstantiatePrefab(CustomEventData customEventData)
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

            gameObject.GetComponentsInChildren<Animator>().Do(n => _instantiator.InstantiateComponent<AnimatorSyncController>(n.gameObject, new object[] { customEventData.time }));
            gameObject.GetComponentsInChildren<ParticleSystem>().Do(n => _instantiator.InstantiateComponent<ParticleSystemSyncController>(n.gameObject, new object[] { customEventData.time }));
            gameObject.GetComponentsInChildren<VideoPlayer>().Do(n => _instantiator.InstantiateComponent<VideoPlayerSyncController>(n.gameObject, new object[] { customEventData.time }));

            string? id = data.Id;
            if (id != null)
            {
                Log.Logger.Log($"Created [{assetName}] with id [{id}].");
                _prefabManager.Add(id, gameObject, data.Track);
            }
            else
            {
                string genericId = gameObject.GetHashCode().ToString();
                Log.Logger.Log($"Created [{assetName}] without id.");
                _prefabManager.Add(genericId, gameObject, data.Track);
            }
        }
    }
}
