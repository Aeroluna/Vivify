using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using IPA.Loader;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace Vivify.ObjectPrefab.Hijackers;

internal class SaberModelControllerHijacker : IHijacker<GameObject>
{
    ////private static readonly List<Component> _nonAllocComponentList = [];

    private readonly IInstantiator _instantiator;
    private readonly HashSet<Renderer> _originalRenderers;
    private readonly Saber _saber;
    private readonly SiraLog _log;
    private readonly SaberModelController _saberModelController;
    private SetSaberGlowColor[]? _cachedSetSaberGlowColors;

    private bool _shouldHide;

    [UsedImplicitly]
    internal SaberModelControllerHijacker(SiraLog log, SaberModelController saberModelController, IInstantiator instantiator)
    {
        _log = log;
        _saberModelController = saberModelController;
        _instantiator = instantiator;
        Transform parent = saberModelController.transform.parent;
        _originalRenderers = parent.GetComponentsInChildren<Renderer>().ToHashSet();
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

        Assembly? assembly = PluginManager.GetPlugin("CustomSabersLite")?.Assembly;
        if (assembly == null)
        {
            return;
        }

        saberModelController.gameObject.AddComponent<LiteModelContract>().Init(assembly, this);
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
            _shouldHide = true;
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

        _shouldHide = false;
        foreach (Renderer renderer in _originalRenderers)
        {
            renderer.enabled = true;
        }
    }

    private class LiteModelContract : MonoBehaviour
    {
        private SaberModelControllerHijacker _hijacker = null!;
        private FieldInfo? _liteSaberInstanceField;
        private SaberModelController? _saberModelController;

        internal void Init(Assembly assembly, SaberModelControllerHijacker hijacker)
        {
            _saberModelController = hijacker._saberModelController;
            Type type = _saberModelController.GetType();
            if (type.Assembly != assembly)
            {
                Destroy(this);
                return;
            }

            _liteSaberInstanceField = type.GetField("liteSaberInstance", AccessTools.all);
            if (_liteSaberInstanceField == null || GetLiteSaberInstance() != null)
            {
                Destroy(this);
                return;
            }

            hijacker._log.Warn("CustomSabersLite model not yet created, deferring renderer fetching");
            _hijacker = hijacker;
        }

        private object? GetLiteSaberInstance()
        {
            return _liteSaberInstanceField?.GetValue(_saberModelController);
        }

        private void OnTransformChildrenChanged()
        {
            object? liteSaberInstance = GetLiteSaberInstance();
            if (liteSaberInstance == null)
            {
                return;
            }

            Destroy(this);

            GameObject? saberGameObject = (GameObject?)liteSaberInstance
                .GetType()
                .GetProperty("GameObject", AccessTools.all)
                ?.GetValue(liteSaberInstance);
            if (saberGameObject == null)
            {
                return;
            }

            HashSet<Renderer> renderers = _hijacker._originalRenderers;
            Renderer[] newRenderers = saberGameObject.GetComponentsInChildren<Renderer>();
            renderers.UnionWith(newRenderers);

            if (_hijacker._shouldHide)
            {
                foreach (Renderer renderer in newRenderers)
                {
                    renderer.enabled = false;
                }
            }

            _hijacker._log.Info("Fetched CustomSabersLite model");
        }
    }
}
