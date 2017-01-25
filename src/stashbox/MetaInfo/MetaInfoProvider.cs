﻿using Stashbox.BuildUp.DelegateFactory;
using Stashbox.Entity;
using Stashbox.Entity.Resolution;
using Stashbox.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Stashbox.MetaInfo
{
    internal class MetaInfoProvider : IMetaInfoProvider
    {
        private readonly IContainerContext containerContext;
        private readonly MetaInfoCache metaInfoCache;
        private readonly Lazy<HashSet<Type>> sensitivityList;

        public Type TypeTo => this.metaInfoCache.TypeTo;

        public bool HasInjectionMethod { get; }

        public bool HasInjectionMembers { get; }

        public HashSet<Type> SensitivityList => this.sensitivityList.Value;

        public MetaInfoProvider(IContainerContext containerContext, MetaInfoCache metaInfoCache)
        {
            this.containerContext = containerContext;
            this.metaInfoCache = metaInfoCache;
            this.HasInjectionMethod = this.metaInfoCache.InjectionMethods.Any();
            this.HasInjectionMembers = this.metaInfoCache.InjectionMembers.Any();
            this.sensitivityList = new Lazy<HashSet<Type>>(() => new HashSet<Type>(this.metaInfoCache.Constructors.SelectMany(constructor => constructor.Parameters, (constructor, parameter) => parameter.Type)
                        .Concat(this.metaInfoCache.InjectionMethods.SelectMany(method => method.Parameters, (method, parameter) => parameter.Type))
                        .Concat(this.metaInfoCache.InjectionMembers.Select(members => members.TypeInformation.Type)).Distinct()));
        }

        public bool TryChooseConstructor(out ResolutionConstructor resolutionConstructor, ResolutionInfo resolutionInfo, InjectionParameter[] injectionParameters = null)
        {
            return this.TryGetConstructor(this.metaInfoCache.Constructors, out resolutionConstructor, resolutionInfo, injectionParameters);
        }

        public IEnumerable<ResolutionMethod> GetResolutionMethods(ResolutionInfo resolutionInfo, InjectionParameter[] injectionParameters = null)
        {
            return this.metaInfoCache.InjectionMethods
               .Select(methodInfo => new ResolutionMethod
               {
                   MethodDelegate = ExpressionDelegateFactory.CreateMethodExpression(this.containerContext,
                     methodInfo.Parameters.Select(parameter =>
                        this.containerContext.ResolutionStrategy.BuildResolutionTarget(this.containerContext, parameter, injectionParameters)).ToArray(),
                    methodInfo.Method),
                   Method = methodInfo.Method,
                   Parameters = methodInfo.Parameters.Select(parameter => this.containerContext.ResolutionStrategy.BuildResolutionTarget(this.containerContext, parameter, injectionParameters)).ToArray()
               });
        }

        public IEnumerable<ResolutionMember> GetResolutionMembers(ResolutionInfo resolutionInfo, InjectionParameter[] injectionParameters = null)
        {
            return this.metaInfoCache.InjectionMembers
                .Select(memberInfo => new ResolutionMember
                {
                    ResolutionTarget = containerContext.ResolutionStrategy.BuildResolutionTarget(containerContext, memberInfo.TypeInformation, injectionParameters),
                    MemberSetter = memberInfo.MemberInfo.GetMemberSetter(),
                    MemberInfo = memberInfo.MemberInfo
                });
        }

        private bool TryGetConstructor(IEnumerable<ConstructorInformation> constructors, out ResolutionConstructor resolutionConstructor,
            ResolutionInfo resolutionInfo, InjectionParameter[] injectionParameters = null)
        {
            var usableConstructors = this.GetUsableConstructors(constructors, resolutionInfo, injectionParameters).ToArray();

            if (usableConstructors.Any())
            {
                resolutionConstructor = this.CreateResolutionConstructor(this.SelectBestConstructor(usableConstructors), injectionParameters);
                return true;
            }

            resolutionConstructor = null;
            return false;
        }

        private IEnumerable<ConstructorInformation> GetUsableConstructors(IEnumerable<ConstructorInformation> constructors, ResolutionInfo resolutionInfo,
            InjectionParameter[] injectionParameters = null)
        {
            return constructors
                .Where(constructor => constructor.Parameters
                    .All(parameter => this.containerContext.ResolutionStrategy.CanResolve(resolutionInfo, this.containerContext, parameter,
                    injectionParameters)));
        }

        private ResolutionConstructor CreateResolutionConstructor(ConstructorInformation constructorInformation, InjectionParameter[] injectionParameters = null)
        {
            return new ResolutionConstructor
            {
                Constructor = constructorInformation.Constructor,
                Parameters = constructorInformation.Parameters.Select(parameter => this.containerContext.ResolutionStrategy.BuildResolutionTarget(this.containerContext, parameter, injectionParameters)).ToArray()
            };
        }

        private ConstructorInformation SelectBestConstructor(IEnumerable<ConstructorInformation> constructors)
        {
            return this.containerContext.ContainerConfiguration.ConstructorSelectionRule(constructors);
        }
    }
}
