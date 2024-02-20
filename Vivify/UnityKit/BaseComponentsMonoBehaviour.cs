using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Vivify.UnityKit
{
    /// <summary>
    /// MonoBehaviour which has base components it would like to initialize.
    /// </summary>
    public abstract class BaseComponentsMonoBehaviour : MonoBehaviour
    {
        private readonly List<object> _baseComponents = new List<object>();

        [Inject]
        private DiContainer _container = null!;

        [Inject]
        private IInstantiator _instantiator = null!;

        /// <summary>
        /// Gets a value indicating whether the base components are initialized or not.
        /// </summary>
        public bool BaseComponentsInitialized { get; private set; }

        /// <summary>
        /// Ensures the base components of our MonoBehaviour is initialized and gets a base component by type.
        /// </summary>
        /// <typeparam name="T">The type of the base component.</typeparam>
        /// <returns>The base component.</returns>
        public T GetBaseComponent<T>()
            where T : Component
        {
            InitializeBaseComponentsInternal();

            foreach (object baseComponent in _baseComponents)
            {
                if (baseComponent.GetType() == typeof(T))
                {
                    return (T)baseComponent;
                }
            }

            throw new Exception("Base component does not exist, even after attempting to initialize forcefully. Perhaps the base component doesn't exist at all?");
        }

        /// <summary>
        /// Used for internal purposes only.
        /// </summary>
        internal void InitializeBaseComponentsInternal()
        {
            if (!BaseComponentsInitialized)
            {
                InitializeBaseComponents();
                BaseComponentsInitialized = true;
            }
        }

        /// <summary>
        /// Initializes the base components of our MonoBehaviour.
        /// </summary>
        protected abstract void InitializeBaseComponents();

        /// <summary>
        /// Gets the Zenject container.
        /// </summary>
        /// <returns>The Zenject container.</returns>
        protected DiContainer GetContainer()
        {
            return _container;
        }

        /// <summary>
        /// Gets the Zenject instantiator.
        /// </summary>
        /// <returns>The Zenject instantiator.</returns>
        protected IInstantiator GetInstantiator()
        {
            return _instantiator;
        }

        /// <summary>
        /// Adds the base component and returns back the same component passed in, useful for chaining.
        /// </summary>
        /// <typeparam name="T">The type of the base component.</typeparam>
        /// <param name="baseComponent">The base component instance.</param>
        /// <returns>The same base component instance passed in.</returns>
        /// <exception cref="Exception">Thrown if base component already exists.</exception>
        protected T AddBaseComponent<T>(T baseComponent)
            where T : Component
        {
            bool alreadyExists = _baseComponents.Contains(baseComponent);
            if (alreadyExists)
            {
                throw new Exception("Base component already exists.");
            }

            _baseComponents.Add(baseComponent);
            return baseComponent;
        }
    }
}
