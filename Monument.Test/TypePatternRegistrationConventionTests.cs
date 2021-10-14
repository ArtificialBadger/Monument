using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monument.SimpleInjector;
using Monument.Conventions;
using Monument.Types;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using Monument.Types.Generic;
using Monument.Types.Utility;

namespace Monument.Test
{
    [TestClass]
    public class TypePatternRegistrationConventionTests
    {
        public static Type GetType<T>(params Type[] types)
            where T : class => Get<T>(types).GetType();

        public static T Get<T>(params Type[] types)
            where T : class => ((new TypePatternRegistrationConvention()
                .Register(types, new SimpleInjectorAdapter(new Container()))) as Monument.Containers.IConvertableContainer)
                .ToRuntimeContainer()
                .Resolve<T>();

        [TestMethod]
        public void BasicRegistration()
        => Assert.AreEqual(typeof(Animal),
            GetType<ISimpleInterface>(
                typeof(Animal)));

        [TestMethod]
        public void BasicDecoratorRegistration()
         => Assert.AreEqual(typeof(SimpleDecorator),
             GetType<ISimpleInterface>(
                 typeof(Animal),
                 typeof(SimpleDecorator)));

        [TestMethod]
        public void BasicCompositeRegistration()
         => Assert.AreEqual(typeof(SimpleComposite),
             GetType<ISimpleInterface>(
                 typeof(SimpleComposite),
                 typeof(Animal),
                 typeof(SimpleImplementation2),
                 typeof(SimpleImplementation3)));

        [TestMethod]
        public void OpenGenericRegistration()
         => Assert.AreEqual(typeof(OpenGenericNode1<string>),
             GetType<IGeneric<string>>(
                 typeof(OpenGenericNode1<>)));

        [TestMethod]
        public void GetTypeKeyReturnsCorrectSignature() =>
            Assert.AreEqual(typeof(IGeneric<>), typeof(IGeneric<string>).ToTypeKey());

        [TestMethod]
        public void OpenGenericDecoratorRegistration()
         => Assert.AreEqual(typeof(OpenGenericDecorator<string>),
             GetType<IGeneric<string>>(
                 typeof(OpenGenericDecorator<>),
                 typeof(OpenGenericNode1<>)));

        [TestMethod]
        public void MixedDecorators()
        {
            var instance = Get<IGeneric<Animal>>(
                typeof(OpenGenericDecorator<>),
                typeof(ClosedGenericDecorator),
                typeof(ClosedGenericNode1));

            var decorator1 = instance as OpenGenericDecorator<Animal> ?? (instance as ClosedGenericDecorator).DecoratedComponent as OpenGenericDecorator<Animal>;
            var decorator2 = instance as ClosedGenericDecorator ?? (instance as OpenGenericDecorator<Animal>).DecoratedComponent as ClosedGenericDecorator;

            Assert.IsNotNull(decorator1);
            Assert.IsNotNull(decorator2);
        }

        [TestMethod]
        public void MixedCollectionOfOpenAndClosedImplementations()
        {
            var instances = Get<IEnumerable<IGeneric<Animal>>>(
                typeof(OpenGenericNode1<>),
                typeof(ClosedGenericNode1));

            var instance2s = Get<IEnumerable<IGeneric<SimpleImplementation2>>>(
                typeof(OpenGenericNode1<>),
                typeof(ClosedGenericNode1));

            Assert.AreEqual(1, instance2s.Count());
            Assert.AreEqual(2, instances.Count());
        }

        [TestMethod]
        public void OpenGenericCompositeAroundMixedCollectionOfOpenAndClosedImplementations() =>
            Assert.AreEqual(typeof(OpenGenericComposite<Animal>), GetType<IGeneric<Animal>>(
                typeof(OpenGenericComposite<>),
                typeof(OpenGenericNode1<>),
                typeof(ClosedGenericNode1)));

        [TestMethod]
        public void CompositeAroundMixedCollectionOfOpenAndClosedImplementations()
        {
            var composite = Get<IGeneric<Animal>>(
                typeof(ClosedGenericComposite),
                typeof(OpenGenericNode1<>),
                typeof(OpenGenericNode2<>),
                typeof(ClosedGenericNode2),
                typeof(ClosedGenericNode4));


            Assert.AreEqual(typeof(ClosedGenericComposite), composite.GetType());
            Assert.AreEqual(4, (composite as ClosedGenericComposite).NodeCount);
        }

        [TestMethod]
        public void CompositeAroundClosedGenericTypes()
        {
            var genericComposite = Get<IGeneric<Animal>>(typeof(ClosedGenericNode1), typeof(ClosedGenericNode2), typeof(ClosedGenericComposite));

            Assert.AreEqual(typeof(ClosedGenericComposite), genericComposite.GetType());
        }

        [TestMethod]
        public void DecoratedCompositeAroundClosedGenericTypes()
        {
            var genericComposite = Get<IGeneric<Animal>>(typeof(ClosedGenericNode1), typeof(ClosedGenericNode2), typeof(ClosedGenericComposite), typeof(ClosedGenericDecorator));

            Assert.AreEqual(typeof(ClosedGenericDecorator), genericComposite.GetType());
        }

        [TestMethod, Ignore]
        public void WhereDoesTheDecoratorHide()
        {
            var instance = Get<IGeneric<Animal>>(
                typeof(ClosedGenericComposite),
                typeof(OpenGenericNode1<>),
                typeof(ClosedGenericDecorator),
                typeof(ClosedGenericNode1));

            Assert.IsTrue(true);

            // that's a problem
        }

        [TestMethod]
        public void OpenGenericCompositeRegistration()
         => Assert.AreEqual(typeof(OpenGenericComposite<string>),
             GetType<IGeneric<string>>(
                 typeof(OpenGenericComposite<>),
                 typeof(OpenGenericNode1<>),
                 typeof(OpenGenericNode2<>)));

        [TestMethod]
        public void ClosedAndOpenGenericListRegistration()
        {
            Assert.AreEqual(2, Get<IEnumerable<IGeneric<SimpleImplementation2>>>(
                typeof(OpenGenericNode1<>),
                typeof(ClosedGenericNode1),
                typeof(ClosedGenericNode4)).Count());
            Assert.AreEqual(2, Get<IEnumerable<IGeneric<Animal>>>(
                typeof(OpenGenericNode1<>),
                typeof(ClosedGenericNode1),
                typeof(ClosedGenericNode4)).Count());
        }

        [TestMethod]
        public void ClosedGenericRegistration()
        {
            Assert.IsNotNull(Get<IGeneric<SimpleImplementation2>>(
                typeof(ClosedGenericNode1),
                typeof(ClosedGenericNode4)));
            Assert.IsNotNull(Get<IGeneric<Animal>>(
                typeof(ClosedGenericNode1),
                typeof(ClosedGenericNode4)));
        }

        [TestMethod]
        public void ClosedGenericListRegistration() =>
            Assert.AreEqual(2, Get<IEnumerable<IGeneric<Animal>>>(
                typeof(ClosedGenericNode1),
                typeof(ClosedGenericNode2)).Count());

        //[TestMethod]
        //public void ClosedGenericAdapterRegistration() =>
        //    Assert.AreEqual(typeof(ClosedGenericAdapter), GetType<IGeneric<Animal>>(
        //        typeof(ClosedGenericAdapter),
        //        typeof(ClosedGenericImplementation4)));
    }
}
