using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Internal;

namespace VContainer.Unity
{
    sealed class FindComponentProvider : IInstanceProvider
    {
        readonly Type componentType;
        readonly IReadOnlyList<IInjectParameter> customParameters;
        ComponentDestination destination;
        Scene scene;
        private bool multiScene;

        public FindComponentProvider(
            Type componentType,
            IReadOnlyList<IInjectParameter> customParameters,
            in Scene scene,
            in ComponentDestination destination)
        {
            this.componentType = componentType;
            this.customParameters = customParameters;
            this.scene = scene;
            this.destination = destination;
        }
        
        public FindComponentProvider(
            Type componentType,
            IReadOnlyList<IInjectParameter> customParameters,
            bool multiScene,
            in ComponentDestination destination)
        {
            this.componentType = componentType;
            this.customParameters = customParameters;
            this.multiScene = multiScene;
            this.destination = destination;
        }

        private Component ResolveInScene(in Scene customScene)
        {
            var component = default(Component);
            var gameObjectBuffer = UnityEngineObjectListBuffer<GameObject>.Get();
            scene.GetRootGameObjects(gameObjectBuffer);
            foreach (var gameObject in gameObjectBuffer)
            {
                component = gameObject.GetComponentInChildren(componentType, true);
                if (component != null) break;
            }

            return component;
        }

        public object SpawnInstance(IObjectResolver resolver)
        {
            var component = default(Component);

            var parent = destination.GetParent();
            if (parent != null)
            {
                component = parent.GetComponentInChildren(componentType, true);
                if (component == null)
                {
                    throw new VContainerException(componentType, $"{componentType} is not in the parent {parent.name} : {this}");
                }
            }
            else if (multiScene)
            {
                for(var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var loadedScene = SceneManager.GetSceneAt(i);
                    component = ResolveInScene(in loadedScene);
                    if (component != null) break;
                }
                if (component == null)
                {
                    throw new VContainerException(componentType, $"{componentType} is not in any scene : {this}");
                }
            }
            else if (scene.IsValid())
            {
                component = ResolveInScene(in scene);
                if (component == null)
                {
                    throw new VContainerException(componentType, $"{componentType} is not in this scene {scene.path} : {this}");
                }
            }
            else
            {
                throw new VContainerException(componentType, $"Invalid Component find target {this}");
            }

            if (component is MonoBehaviour monoBehaviour)
            {
                var injector = InjectorCache.GetOrBuild(monoBehaviour.GetType());
                injector.Inject(monoBehaviour, resolver, customParameters);
            }

            destination.ApplyDontDestroyOnLoadIfNeeded(component);
            return component;
        }
    }
}