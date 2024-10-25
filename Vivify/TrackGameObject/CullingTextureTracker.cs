using System.Collections.Generic;
using System.Linq;
using Heck.Animation;
using UnityEngine;
using Vivify.Controllers;

namespace Vivify.TrackGameObject;

internal sealed class CullingTextureTracker : TrackGameObjectTracker
{
    private readonly HashSet<RendererController> _maskRenderers = [];

    private GameObject[] _gameObjects = [];

    private bool _gameObjectsDirty;

    internal CullingTextureTracker(IEnumerable<Track> tracks, bool whitelist)
        : base(tracks)
    {
        Whitelist = whitelist;
        UpdateGameObjects();
    }

    internal GameObject[] GameObjects
    {
        get
        {
            if (_gameObjectsDirty)
            {
                _gameObjects = _maskRenderers.SelectMany(n => n.ChildRenderers).Select(n => n.gameObject).ToArray();
            }

            return _gameObjects;
        }
    }

    internal bool Whitelist { get; }

    public override void Dispose()
    {
        base.Dispose();

        foreach (RendererController maskRenderer in _maskRenderers)
        {
            maskRenderer.OnDestroyed -= OnMaskRendererDestroyed;
            maskRenderer.OnTransformChanged -= UpdateGameObjects;
        }
    }

    protected override void OnGameObjectAdded(GameObject gameObject)
    {
        RendererController? maskRenderer = gameObject.GetComponent<RendererController>();
        maskRenderer ??= gameObject.AddComponent<RendererController>();

        _maskRenderers.Add(maskRenderer);
        maskRenderer.OnDestroyed += OnMaskRendererDestroyed;
        maskRenderer.OnTransformChanged += UpdateGameObjects;

        UpdateGameObjects();
    }

    protected override void OnGameObjectRemoved(GameObject gameObject)
    {
        RendererController rendererController = gameObject.GetComponent<RendererController>();
        if (rendererController != null)
        {
            OnMaskRendererDestroyed(rendererController);
        }
    }

    private void OnMaskRendererDestroyed(RendererController rendererController)
    {
        _maskRenderers.Remove(rendererController);
        rendererController.OnDestroyed -= OnMaskRendererDestroyed;
        rendererController.OnTransformChanged -= UpdateGameObjects;

        UpdateGameObjects();
    }

    private void UpdateGameObjects()
    {
        _gameObjectsDirty = true;
    }
}
