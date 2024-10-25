using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Vivify.Controllers;
using Vivify.TrackGameObject;
using Zenject;

namespace Vivify.Managers;

[UsedImplicitly]
internal class CameraPropertyManager : IInitializable, IDisposable
{
    private static readonly HashSet<CameraPropertyController> _allControllers = [];

    internal static event Action<CameraPropertyController>? ControllerAdded;

    internal static event Action<CameraPropertyController>? ControllerRemoved;

    internal Dictionary<string, CameraProperties> Properties { get; } = [];

    public void Initialize()
    {
        foreach (CameraPropertyController controller in _allControllers)
        {
            OnControllerAdded(controller);
        }

        ControllerAdded += OnControllerAdded;
        ControllerRemoved += OnControllerRemoved;
    }

    public void Dispose()
    {
        foreach (CameraProperties properties in Properties.Values)
        {
            properties.Dispose();
        }

        foreach (CameraPropertyController controller in _allControllers)
        {
            OnControllerRemoved(controller);
        }

        ControllerAdded -= OnControllerAdded;
        ControllerRemoved -= OnControllerRemoved;
    }

    internal static void AddControllerStatic(CameraPropertyController controller)
    {
        _allControllers.Add(controller);
        ControllerAdded?.Invoke(controller);
    }

    internal static void RemoveControllerStatic(CameraPropertyController controller)
    {
        _allControllers.Remove(controller);
        ControllerRemoved?.Invoke(controller);
    }

    private void OnControllerAdded(CameraPropertyController controller)
    {
        string id = controller.Id ?? VivifyController.CAMERA_TARGET;
        if (!Properties.TryGetValue(
                id,
                out CameraProperties properties))
        {
            Properties[id] = properties = new CameraProperties();
        }

        properties.AddController(controller);
    }

    private void OnControllerRemoved(CameraPropertyController controller)
    {
        if (Properties.TryGetValue(
                controller.Id ?? VivifyController.CAMERA_TARGET,
                out CameraProperties properties))
        {
            properties.RemoveController(controller);
        }
    }

    internal class CameraProperties : IDisposable
    {
        private readonly HashSet<CameraPropertyController> _controllers = [];

        private DepthTextureMode? _depthTextureMode;
        private CameraClearFlags? _clearFlags;
        private Color? _backgroundColor;
        private CullingTextureTracker? _cullingTextureData;

        internal DepthTextureMode? DepthTextureMode
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

        internal CameraClearFlags? ClearFlags
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

        internal Color? BackgroundColor
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

        internal CullingTextureTracker? CullingTextureData
        {
            set
            {
                _cullingTextureData = value;
                foreach (CameraPropertyController controller in _controllers)
                {
                    controller.CullingTextureData = value;
                }
            }
        }

        public void Dispose()
        {
            _cullingTextureData?.Dispose();
        }

        internal void AddController(CameraPropertyController controller)
        {
            controller.DepthTextureMode = _depthTextureMode;
            controller.ClearFlags = _clearFlags;
            controller.BackgroundColor = _backgroundColor;
            controller.CullingTextureData = _cullingTextureData;
            _controllers.Add(controller);
        }

        internal void RemoveController(CameraPropertyController controller)
        {
            _controllers.Remove(controller);
            controller.Reset();
        }
    }
}
