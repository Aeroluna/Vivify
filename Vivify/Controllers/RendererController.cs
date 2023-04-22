using System;
using UnityEngine;

namespace Vivify.Controllers
{
    internal class RendererController : MonoBehaviour
    {
        internal event Action<RendererController>? OnDestroyed;

        internal event Action? OnTransformChanged;

        internal Renderer[] ChildRenderers { get; private set; } = Array.Empty<Renderer>();

        private void OnEnable()
        {
            UpdateChildRenderers();
        }

        private void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
        }

        private void OnTransformChildrenChanged()
        {
            UpdateChildRenderers();
            OnTransformChanged?.Invoke();
        }

        private void UpdateChildRenderers()
        {
            ChildRenderers = transform.GetComponentsInChildren<Renderer>(true);
        }
    }
}
