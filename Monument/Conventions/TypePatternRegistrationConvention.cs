﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Monument.Attributes;
using Monument.Containers;
using Monument.Factories;

namespace Monument.Conventions
{
    public class TypePatternRegistrationConvention : IRegistrationConvention
    {
        public IRegisterTimeContainer Register(IEnumerable<Type> types, IRegisterTimeContainer container)
        {
            return RegisterTypes(types, container);
        }

        public static IRegisterTimeContainer RegisterTypes(IEnumerable<Type> types, IRegisterTimeContainer container)
        {
            var implementationTypes = types
                .Where(type => !type.IsInterface)
                .Where(type => !type.IsAbstract)
                .Where(type => type.IsPublic)
                .Where(type => !type.IsNested)
                .Where(type => type.GetInterfaces().Count() == 1)
                .Where(type => type.GetConstructors().Count() == 1)
                .Where(type => !type.CustomAttributes.Any(a => a.AttributeType == typeof(IgnoreAttribute)));

            var byInterface = implementationTypes
                .GroupBy(type => type.GetInterfaces().Single().ToTypeKey());

            foreach (var implementations in byInterface)
            {
                var inter = implementations.Key;

                var composites = implementations.Where(type => type.IsComposite());
                var decorators = implementations.Where(type => type.IsDecorator());

                var normals = implementations.Except(composites).Except(decorators);

                if (composites.Count() > 1)
                {
                    throw new TypePatternRegistrationException("You cannot register more than one composite.");
                }
                if (!composites.Any() && !normals.Any())
                {
                    throw new TypePatternRegistrationException("You cannot register only decorators.");
                }

                if (composites.Any())
                {
                    container.Register(inter, composites.Single());
                }
                else
                {
                    if (inter.IsGenericType)
                    {
                        foreach (var normalSingleGroup in normals.GroupBy(type => type.GetInterfaces().Single().GetGenericArguments(), new TypeArrayEqualityComparer()))
                        {
                            if (normalSingleGroup.Count() == 1)
                            {
                                var normal = normalSingleGroup.Single();

                                if (!normal.IsOpenGeneric() && !normals.Any(n => n.IsOpenGeneric()))
                                {
                                    container.Register(normal.GetInterfaces().Single(), normal);
                                }
                                else if (normal.IsOpenGeneric())
                                {
                                    container.Register(inter, normal);
                                }

                            }
                        }
                    }
                    else
                    {
                        if (normals.Count() == 1)
                        {
                            container.Register(inter, normals.Single());
                            //container.Register(typeof(IFactory<>).GetGenericTypeDefinition().MakeGenericType(inter), typeof(Factory<>).GetGenericTypeDefinition().MakeGenericType(normals.Single()));
                        }
                    }

                }

                // What is this thing for? The above should handle it?
                if (normals.Any())
                {
                    container.RegisterAll(inter, normals);
                }

                foreach (var decorator in decorators)
                {
                    container.RegisterDecorator(inter, decorator);
                }
            }

            return container;
        }

        // To Fluent or not to Fluent? That is occasionally the question
        //public void RegisterFactory(IRegisterTimeContainer registerTimeContainer, IRuntimeContainer runtimeContainer)
        //{
        //    registerTimeContainer.Register(typeof(IRuntimeContainer), () => runtimeContainer, Lifestyle.Singleton);
        //    registerTimeContainer.Register(typeof(IFactory<>), typeof(Factory<>));
        //}

        public static void RegisterFactory(IRegisterTimeContainer registerTimeContainer, IRuntimeContainer runtimeContainer)
        {
            registerTimeContainer.Register(typeof(IRuntimeContainer), () => runtimeContainer, Lifestyle.Singleton);
            registerTimeContainer.Register(typeof(IFactory<>), typeof(Factory<>));
        }    

        private class TypeArrayEqualityComparer : IEqualityComparer<Type[]>
        {
            public bool Equals(Type[] x, Type[] y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(Type[] obj)
            {
                unchecked
                {
                    int hash = 17;

                    foreach (var item in obj)
                    {
                        hash = hash * 23 + item.GetHashCode();
                    }

                    return hash;
                }
            }
        }
    }
}
