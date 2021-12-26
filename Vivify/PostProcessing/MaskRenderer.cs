using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vivify.PostProcessing
{
    internal class MaskRenderer : MonoBehaviour
    {
        internal event Action<MaskRenderer>? OnDestroyed;

        internal event Action? OnTransformChanged;

        internal List<Renderer> ChildRenderers { get; } = new();

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
            ChildRenderers.Clear();
            FindRenderers(transform);
        }

        private void FindRenderers(Transform target)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                ChildRenderers.Add(renderer);
            }

            // include children
            foreach (Transform child in target)
            {
                if (child.GetComponent<MaskRenderer>() == null)
                {
                    FindRenderers(child);
                }
            }
        }
    }
}
