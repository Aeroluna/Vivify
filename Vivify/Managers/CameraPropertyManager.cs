using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Vivify.Controllers;
using Zenject;

namespace Vivify.Managers;

[UsedImplicitly]
internal class CameraPropertyManager : IInitializable, IDisposable
{
    private static readonly HashSet<CameraPropertyController> _controllers = [];

    private DepthTextureMode? _depthTextureMode;
    private CameraClearFlags? _clearFlags;
    private Color? _backgroundColor;

    internal static event Action<CameraPropertyController>? ControllerAdded;

    internal static event Action<CameraPropertyController>? ControllerRemoved;

    internal DepthTextureMode DepthTextureMode
    {
        set
        {
            _depthTextureMode = value;
            foreach (CameraPropertyController controller in _controllers)
            {
                controller.DepthTextureMode = value;
            }
        }
    }

    internal CameraClearFlags ClearFlags
    {
        set
        {
            _clearFlags = value;
            foreach (CameraPropertyController controller in _controllers)
            {
                controller.ClearFlags = value;
            }
        }
    }

    internal Color BackgroundColor
    {
        set
        {
            _backgroundColor = value;
            foreach (CameraPropertyController controller in _controllers)
            {
                controller.BackgroundColor = value;
            }
        }
    }

    public void Initialize()
    {
        ControllerAdded += OnControllerAdded;
        ControllerRemoved += OnControllerRemoved;
    }

    public void Dispose()
    {
        foreach (CameraPropertyController controller in _controllers)
        {
            OnControllerRemoved(controller);
        }

        ControllerAdded -= OnControllerAdded;
        ControllerRemoved -= OnControllerRemoved;
    }

    internal static void AddController(CameraPropertyController controller)
    {
        _controllers.Add(controller);
        ControllerAdded?.Invoke(controller);
    }

    internal static void RemoveController(CameraPropertyController controller)
    {
        _controllers.Remove(controller);
        ControllerRemoved?.Invoke(controller);
    }

    private void OnControllerAdded(CameraPropertyController controller)
    {
        controller.DepthTextureMode = _depthTextureMode;
        controller.ClearFlags = _clearFlags;
        controller.BackgroundColor = _backgroundColor;
    }

    private void OnControllerRemoved(CameraPropertyController controller)
    {
        controller.Reset();
    }
}
