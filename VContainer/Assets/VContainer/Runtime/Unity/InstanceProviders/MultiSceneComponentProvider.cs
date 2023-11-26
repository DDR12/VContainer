using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Internal;

namespace VContainer.Unity
{
    sealed class MultiSceneComponentProvider : IInstanceProvider
    {
        readonly Type componentType;
        readonly IReadOnlyList<IInjectParameter> customParameters;
        private bool includeInactive = false;
        public MultiSceneComponentProvider(
            Type componentType,
            IReadOnlyList<IInjectParameter> customParameters,
            bool includeInactive)
        {
            this.componentType = componentType;
            this.customParameters = customParameters;
            this.includeInactive = includeInactive;
        }

        public object SpawnInstance(IObjectResolver resolver)
        {
            var result = UnityEngine.Object.FindObjectOfType(componentType, includeInactive)
                as Component;
            if (result == null)
            {
                throw new VContainerException(componentType, $"{componentType} is not found in the loaded scenes : {this}");
            }
            if (result is MonoBehaviour monoBehaviour)
            {
                var injector = InjectorCache.GetOrBuild(monoBehaviour.GetType());
                injector.Inject(monoBehaviour, resolver, customParameters);
            }

            return result;
        }
    }
}