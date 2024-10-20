using System.Collections.Generic;
using UnityEngine;
using Vivify.TrackGameObject;
using static Vivify.VivifyController;

namespace Vivify.Controllers;

internal abstract class CullingCameraController : MonoBehaviour
{
    private readonly HashSet<(GameObject, int)> _cachedLayers = [];

    private Camera? _camera;

    private CullingTextureTracker? _cullingTextureData;

    internal abstract int DefaultCullingMask { get; }

    internal Camera Camera => _camera ??= GetComponent<Camera>();

    internal CullingTextureTracker? CullingTextureData
    {
        get => _cullingTextureData;

        set
        {
            _cullingTextureData = value;
            RefreshCullingMask();
        }
    }

    protected void RefreshCullingMask()
    {
        // flip culling mask when whitelist mode enabled
        Camera.cullingMask = _cullingTextureData?.Whitelist ?? false ? 1 << CULLING_LAYER : DefaultCullingMask;
    }

    protected virtual void OnPreCull()
    {
        if (_cullingTextureData == null)
        {
            return;
        }

        // Set renderers to culling layer
        GameObject[] gameObjects = _cullingTextureData.GameObjects;
        int length = gameObjects.Length;
        for (int i = 0; i < length; i++)
        {
            GameObject renderedObject = gameObjects[i];
            _cachedLayers.Add((renderedObject, renderedObject.layer));
            renderedObject.layer = CULLING_LAYER;
        }
    }

    private void OnPostRender()
    {
        if (_cachedLayers.Count == 0)
        {
            return;
        }

        // reset renderer layers
        foreach ((GameObject? cachedObject, int layer) in _cachedLayers)
        {
            cachedObject.layer = layer;
        }

        _cachedLayers.Clear();
    }
}
