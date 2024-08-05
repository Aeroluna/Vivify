using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vivify.ObjectPrefab.Hijackers;

internal class MpbControllerHijacker : IHijacker<GameObject>
{
    private readonly MaterialPropertyBlockController _materialPropertyBlockController;
    private readonly Renderer[] _originalRenderers;
    private List<int>? _cachedNumberOfMaterialsInRenderers;
    private Renderer[]? _cachedRenderers;

    internal MpbControllerHijacker(Component component)
    {
        _originalRenderers = component.GetComponentsInChildren<Renderer>();

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (component is GameNoteController or BurstSliderGameNoteController)
        {
            _materialPropertyBlockController =
                component.transform.GetChild(0).GetComponent<MaterialPropertyBlockController>();
        }
        else
        {
            _materialPropertyBlockController = component.GetComponent<MaterialPropertyBlockController>();
        }
    }

    public void Activate(List<GameObject> gameObjects, bool hideOriginal)
    {
        if (_materialPropertyBlockController._isInitialized)
        {
            _cachedNumberOfMaterialsInRenderers =
                _materialPropertyBlockController._numberOfMaterialsInRenderers;
            _materialPropertyBlockController._isInitialized = false;
        }

        _cachedRenderers = _materialPropertyBlockController._renderers;
        IEnumerable<Renderer> newRenderers =
            gameObjects.SelectMany(n => n.GetComponentsInChildren<Renderer>(true));

        if (hideOriginal)
        {
            foreach (Renderer renderer in _originalRenderers)
            {
                renderer.enabled = false;
            }

            _materialPropertyBlockController._renderers = newRenderers.ToArray();
        }
        else
        {
            _materialPropertyBlockController._renderers = _cachedRenderers.Concat(newRenderers).ToArray();
        }

        _materialPropertyBlockController.ApplyChanges();
    }

    public void Deactivate()
    {
        if (_cachedNumberOfMaterialsInRenderers != null)
        {
            _materialPropertyBlockController._numberOfMaterialsInRenderers =
                _cachedNumberOfMaterialsInRenderers;
            _cachedNumberOfMaterialsInRenderers = null;
        }

        if (_cachedRenderers != null)
        {
            _materialPropertyBlockController._renderers = _cachedRenderers;
            _cachedRenderers = null;
        }

        foreach (Renderer renderer in _originalRenderers)
        {
            renderer.enabled = true;
        }
    }
}
