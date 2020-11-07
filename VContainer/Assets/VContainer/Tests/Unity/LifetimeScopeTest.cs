using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using VContainer.Unity;

namespace VContainer.Tests.Unity
{
    public class LifetimeScopeTest
    {
        [Test]
        public void PushParent()
        {
            var parent = new GameObject("LifetimeScope").AddComponent<LifetimeScope>();

            using (LifetimeScope.PushParent(parent))
            {
                var child = new GameObject("LifetimeScope Child 1").AddComponent<LifetimeScope>();
                Assert.That(child.Parent, Is.EqualTo(parent));
            }

            var child2 = new GameObject("LifetimeScope Child 2").AddComponent<LifetimeScope>();
            Assert.That(child2.Parent, Is.Null);
        }

        [UnityTest]
        public IEnumerator CreateChild()
        {
            LifetimeScope parentLifetimeScope;

            using (LifetimeScope.Push(builder =>
            {
                builder.RegisterEntryPoint<SampleEntryPoint>(Lifetime.Scoped).AsSelf();
            }))
            {
                parentLifetimeScope = new GameObject("LifetimeScope").AddComponent<LifetimeScope>();
            }

            yield return null;
            yield return null;

            var parentEntryPoint = parentLifetimeScope.Container.Resolve<SampleEntryPoint>();
            var parentLifetimeScopeFromContainer = parentLifetimeScope.Container.Resolve<LifetimeScope>();

            Assert.That(parentLifetimeScopeFromContainer, Is.EqualTo(parentLifetimeScope));
            Assert.That(parentLifetimeScopeFromContainer.transform.childCount, Is.Zero);
            Assert.That(parentEntryPoint, Is.InstanceOf<SampleEntryPoint>());
            Assert.That(parentEntryPoint.InitializeCalled, Is.True);
            Assert.That(parentEntryPoint.TickCalls, Is.EqualTo(2));

            var childLifetimeScope = parentLifetimeScope.CreateChild(builder =>
            {
                builder.RegisterEntryPoint<SampleEntryPoint>(Lifetime.Scoped).AsSelf();
                builder.Register<DisposableServiceA>(Lifetime.Scoped);
            });

            yield return null;
            yield return null;

            var childEntryPoint = childLifetimeScope.Container.Resolve<SampleEntryPoint>();
            var childDisposable = childLifetimeScope.Container.Resolve<DisposableServiceA>();
            var childLifetimeScopeFromContainer = childLifetimeScope.Container.Resolve<LifetimeScope>();

            Assert.That(childLifetimeScopeFromContainer, Is.EqualTo(childLifetimeScope));
            Assert.That(childEntryPoint, Is.InstanceOf<SampleEntryPoint>());
            Assert.That(childEntryPoint, Is.Not.EqualTo(parentEntryPoint));
            Assert.That(childEntryPoint.InitializeCalled, Is.True);
            Assert.That(childEntryPoint.TickCalls, Is.EqualTo(2));

            childLifetimeScope.Dispose();
            yield return null;
            Assert.That(childDisposable.Disposed, Is.True);
            Assert.That(childLifetimeScope == null, Is.True);
            Assert.That(parentLifetimeScope.transform.childCount, Is.Zero);
        }

        [Test]
        public void CreateScopeWithSingleton()
        {
            LifetimeScope parentLifetimeScope;

            using (LifetimeScope.Push(builder =>
            {
                builder.RegisterEntryPoint<SampleEntryPoint>(Lifetime.Singleton).AsSelf();
            }))
            {
                parentLifetimeScope = new GameObject("LifetimeScope").AddComponent<LifetimeScope>();
            }

            var parentEntryPoint = parentLifetimeScope.Container.Resolve<SampleEntryPoint>();
            Assert.That(parentEntryPoint, Is.InstanceOf<SampleEntryPoint>());

            var childLifetimeScope = parentLifetimeScope.CreateChild();
            var childEntryPoint = childLifetimeScope.Container.Resolve<SampleEntryPoint>();

            Assert.That(childEntryPoint, Is.InstanceOf<SampleEntryPoint>());
            Assert.That(childEntryPoint, Is.EqualTo(parentEntryPoint));
        }
    }
}
