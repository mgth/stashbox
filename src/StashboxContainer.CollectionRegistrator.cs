﻿using Stashbox.Registration.Fluent;
using Stashbox.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Stashbox
{
    public partial class StashboxContainer
    {
        /// <inheritdoc />
        public IStashboxContainer RegisterTypesAs(Type typeFrom, IEnumerable<Type> types, Func<Type, bool> selector = null, Action<RegistrationConfigurator> configurator = null)
        {
            Shield.EnsureNotNull(typeFrom, nameof(typeFrom));
            Shield.EnsureNotNull(types, nameof(types));

            types = selector != null ? types.Where(selector).Where(t => t.IsResolvableType()) : types.Where(t => t.IsResolvableType());

            foreach (var type in types)
            {
                if (type.ImplementsWithoutGenericCheck(typeFrom))
                {
                    this.RegisterTypeAs(typeFrom, type, configurator);
                    continue;
                }

                var typeFromInfo = typeFrom.GetTypeInfo();
                if (!typeFromInfo.IsGenericTypeDefinition) continue;

                var typeInfo = type.GetTypeInfo();

                var serviceTypes = type.GetRegisterableBaseTypes().Concat(type.GetRegisterableInterfaceTypes())
                    .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeFrom);

                foreach (var service in serviceTypes)
                    this.RegisterTypeAs(typeInfo.IsGenericTypeDefinition ? typeFrom : service, type, configurator);
            }

            return this;
        }

        /// <inheritdoc />
        public IStashboxContainer RegisterTypes(IEnumerable<Type> types, Func<Type, bool> selector = null, Action<RegistrationConfigurator> configurator = null)
        {
            Shield.EnsureNotNull(types, nameof(types));

            types = selector != null ? types.Where(selector).Where(t => t.IsResolvableType()) : types.Where(t => t.IsResolvableType());

            foreach (var type in types)
            {
                var serviceTypes = type.GetRegisterableBaseTypes().Concat(type.GetRegisterableInterfaceTypes());

                var typeInfo = type.GetTypeInfo();
                if (typeInfo.IsGenericTypeDefinition)
                    serviceTypes = serviceTypes.Where(t =>
                    {
                        var ti = t.GetTypeInfo();
                        if (!ti.IsGenericType) return false;
                        var args = ti.IsGenericTypeDefinition ? ti.GenericTypeParameters : ti.GenericTypeArguments;
                        return typeInfo.GenericTypeParameters.Length == args.Length;
                    }).Select(t => t.GetGenericTypeDefinition());

                foreach (var service in serviceTypes)
                    this.Register(service, type, configurator);

                this.Register(type, configurator);
            }

            return this;
        }

        /// <inheritdoc />
        public IStashboxContainer ComposeBy(Type compositionRootType, params object[] compositionRootArguments)
        {
            this.ThrowIfDisposed();
            Shield.EnsureNotNull(compositionRootType, nameof(compositionRootType));
            Shield.EnsureTrue(compositionRootType.IsCompositionRoot(), $"The given type {compositionRootType} doesn't implement ICompositionRoot.");

            var compositionRoot = (ICompositionRoot)this.Activate(compositionRootType, compositionRootArguments);
            compositionRoot.Compose(this);

            return this;
        }

        /// <inheritdoc />
        public IStashboxContainer ComposeBy(ICompositionRoot compositionRoot)
        {
            this.ThrowIfDisposed();
            compositionRoot.Compose(this);
            return this;
        }

        private void RegisterTypeAs(Type typeFrom, Type type, Action<RegistrationConfigurator> configurator)
        {
            if (configurator == null)
                this.Register(typeFrom, type);
            else
                this.Register(typeFrom, type, configurator);
        }
    }
}
