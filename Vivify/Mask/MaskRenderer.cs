namespace Vivify
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    internal class MaskRenderer : MonoBehaviour
    {
        internal event Action<MaskRenderer>? OnDestroyed;

        internal List<Renderer> ChildRenderers { get; } = new List<Renderer>();

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
        }

        private void UpdateChildRenderers()
        {
            ChildRenderers.Clear();
            FindRenderers(transform);
        }

        private void FindRenderers(Transform transform)
        {
            Renderer renderer = transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                ChildRenderers.Add(renderer);
            }

            // include children
            foreach (Transform child in transform)
            {
                if (child.GetComponent<MaskRenderer>() == null)
                {
                    FindRenderers(child);
                }
            }
        }
    }
}
