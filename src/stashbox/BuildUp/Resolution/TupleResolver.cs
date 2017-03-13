﻿using Stashbox.Infrastructure.Resolution;
using System;
using Stashbox.Entity;
using Stashbox.Infrastructure;
using System.Linq.Expressions;
using System.Collections.Generic;
using Stashbox.Exceptions;

namespace Stashbox.BuildUp.Resolution
{
    internal class TupleResolver : Resolver
    {
        private readonly HashSet<Type> supportedTypes = new HashSet<Type>
        {
            typeof(Tuple<>),
            typeof(Tuple<,>),
            typeof(Tuple<,,>),
            typeof(Tuple<,,,>),
            typeof(Tuple<,,,,>),
            typeof(Tuple<,,,,,>),
            typeof(Tuple<,,,,,,>),
            typeof(Tuple<,,,,,,,>)
        };

        public override bool CanUseForResolution(IContainerContext containerContext, TypeInformation typeInfo) =>
            typeInfo.Type.IsClosedGenericType() && this.supportedTypes.Contains(typeInfo.Type.GetGenericTypeDefinition());

        public override Expression GetExpression(IContainerContext containerContext, TypeInformation typeInfo, ResolutionInfo resolutionInfo)
        {
            var args = typeInfo.Type.GetGenericArguments();
            var tupleConstructor = typeInfo.Type.GetConstructor(args);
            var length = args.Length;
            var expressions = new Expression[length];
            for (var i = 0; i < length; i++)
            {
                var argumentInfo = new TypeInformation { Type = args[i] };
                var expr = containerContext.ResolutionStrategy.BuildResolutionExpression(containerContext, resolutionInfo, argumentInfo, null);
                expressions[i] = expr ?? throw new ResolutionFailedException(typeInfo.Type.FullName);
            }

            return Expression.New(tupleConstructor, expressions);
        }
    }
}