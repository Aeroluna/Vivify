using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Vivify.ObjectPrefab.Hijackers;

internal class SaberModelControllerHijacker : IHijacker<GameObject>
{
    ////private static readonly List<Component> _nonAllocComponentList = [];

    private readonly IInstantiator _instantiator;
    private readonly Renderer[] _originalRenderers;
    private readonly Saber _saber;
    private readonly SaberModelController _saberModelController;
    private SetSaberGlowColor[]? _cachedSetSaberGlowColors;

    [UsedImplicitly]
    internal SaberModelControllerHijacker(SaberModelController saberModelController, IInstantiator instantiator)
    {
        _saberModelController = saberModelController;
        _instantiator = instantiator;
        Transform parent = saberModelController.transform.parent;
        _originalRenderers = parent.GetComponentsInChildren<Renderer>();
        _saber = parent.GetComponent<Saber>();

        /*Assembly? reeSabers = IPA.Loader.PluginManager.GetPlugin("ReeSabers")?.Assembly;
        if (reeSabers != null)
        {
            _originalRenderers = _originalRenderers.Where(
                n =>
                {
                    n.GetComponents(_nonAllocComponentList);
                    return _nonAllocComponentList.All(
                        m =>
                        {
                            string fullName = m.GetType().FullName ?? string.Empty;
                            return fullName != "ReeSabers.Trails.SimpleTrail";
                        });
                }).ToArray();
        }*/
    }

    public void Activate(List<GameObject> gameObjects, bool hideOriginal)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            gameObject.transform.SetParent(_saber.transform, false);
        }

        _cachedSetSaberGlowColors = _saberModelController._setSaberGlowColors;

        SetSaberGlowColor[] newColors = new SetSaberGlowColor[gameObjects.Count];
        for (int i = 0; i < gameObjects.Count; i++)
        {
            GameObject gameObject = gameObjects[i];
            SetSaberManyColor? setSaberManyColor = gameObject.GetComponent<SetSaberManyColor>();
            if (setSaberManyColor == null)
            {
                setSaberManyColor = _instantiator.InstantiateComponent<SetSaberManyColor>(gameObject);
            }

            newColors[i] = setSaberManyColor;
            setSaberManyColor.saberType = _saber.saberType;
        }

        if (hideOriginal)
        {
            foreach (Renderer renderer in _originalRenderers)
            {
                renderer.enabled = false;
            }

            _saberModelController._setSaberGlowColors = newColors;
        }
        else
        {
            _saberModelController._setSaberGlowColors = _cachedSetSaberGlowColors.Concat(newColors).ToArray();
        }
    }

    public void Deactivate()
    {
        if (_cachedSetSaberGlowColors != null)
        {
            _saberModelController._setSaberGlowColors = _cachedSetSaberGlowColors;
            _cachedSetSaberGlowColors = null;
        }

        foreach (Renderer renderer in _originalRenderers)
        {
            renderer.enabled = true;
        }
    }
}
