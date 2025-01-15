using System.Collections.Generic;
using UnityEngine;
using Vivify.TrackGameObject;
using static Vivify.VivifyController;

namespace Vivify.PostProcessing;

internal abstract class CullingCameraController : MonoBehaviour
{
    private readonly HashSet<(GameObject, int)> _cachedLayers = [];

    private Camera? _camera;

    private int? _cachedMask;

    internal bool MainEffect { get; set; }

    internal Camera Camera => _camera ??= GetComponent<Camera>();

    internal CullingTextureTracker? CullingTextureData { get; set; }

    protected virtual void OnPreCull()
    {
        // flip culling mask when whitelist mode enabled
        if (CullingTextureData?.Whitelist ?? false)
        {
            _cachedMask = Camera.cullingMask;
            Camera.cullingMask = 1 << CULLING_LAYER;
        }

        if (CullingTextureData == null)
        {
            return;
        }

        // Set renderers to culling layer
        GameObject[] gameObjects = CullingTextureData.GameObjects;
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
        if (_cachedMask != null)
        {
            Camera.cullingMask = _cachedMask.Value;
        }

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
