using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Vivify.ObjectPrefab.Hijackers
{
    internal class SaberModelControllerHijacker : IHijacker<GameObject>
    {
        private readonly IInstantiator _instantiator;
        private readonly Renderer[] _originalRenderers;
        private readonly Saber _saber;
        private readonly SaberModelController _saberModelController;
        private SetSaberGlowColor[]? _cachedSetSaberGlowColors;

        internal SaberModelControllerHijacker(SaberModelController saberModelController, IInstantiator instantiator)
        {
            _saberModelController = saberModelController;
            _instantiator = instantiator;
            _originalRenderers = saberModelController.GetComponentsInChildren<Renderer>();
            _saber = saberModelController.transform.parent.GetComponent<Saber>();
        }

        public void Activate(List<GameObject> gameObjects, bool hideOriginal)
        {
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
}
