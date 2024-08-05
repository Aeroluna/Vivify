using HarmonyLib;
using UnityEngine;
using UnityEngine.Video;
using Zenject;

namespace Vivify.Controllers.Sync;

internal static class SyncExtensions
{
    internal static void SongSynchronize(this IInstantiator instantiator, GameObject gameObject, float startTime)
    {
        ISync[] syncs = gameObject.GetComponentsInChildren<ISync>();

        if (syncs.Length > 0)
        {
            foreach (ISync sync in syncs)
            {
                sync.SetStartTime(startTime);
            }
        }
        else
        {
            gameObject
                .GetComponentsInChildren<Animator>()
                .Do(
                    n =>
                        instantiator.InstantiateComponent<AnimatorSyncController>(
                            n.gameObject,
                            new object[] { startTime }));
            gameObject
                .GetComponentsInChildren<ParticleSystem>()
                .Do(
                    n =>
                        instantiator.InstantiateComponent<ParticleSystemSyncController>(
                            n.gameObject,
                            new object[] { startTime }));
            gameObject
                .GetComponentsInChildren<VideoPlayer>()
                .Do(
                    n =>
                    {
                        if (n.playOnAwake)
                        {
                            instantiator.InstantiateComponent<VideoPlayerSyncController>(
                                n.gameObject,
                                new object[] { startTime });
                        }
                    });
        }
    }
}
