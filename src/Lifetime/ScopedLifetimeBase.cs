﻿using Stashbox.BuildUp;
using Stashbox.Registration;
using Stashbox.Resolution;
using System;
using System.Linq.Expressions;

namespace Stashbox.Lifetime
{
    /// <summary>
    /// Represents a shared base class for scoped lifetimes.
    /// </summary>
    public abstract class ScopedLifetimeBase : LifetimeBase
    {
        /// <summary>
        /// The id of the scope.
        /// </summary>
        protected readonly object ScopeId = new object();

        /// <summary>
        /// Produces a cached factory delegate to create scoped instances.
        /// </summary>
        /// <param name="containerContext">The container context.</param>
        /// <param name="serviceRegistration">The service registration.</param>
        /// <param name="objectBuilder">The object builder.</param>
        /// <param name="resolutionContext">The resolution context.</param>
        /// <param name="resolveType">The resolve type.</param>
        /// <returns></returns>
        public Func<IResolutionScope, object> GetFactoryDelegate(IContainerContext containerContext, IServiceRegistration serviceRegistration, IObjectBuilder objectBuilder, ResolutionContext resolutionContext, Type resolveType)
        {
            var expr = base.GetExpression(containerContext, serviceRegistration, objectBuilder, resolutionContext, resolveType);
            return expr?.CompileDelegate(resolutionContext);
        }

        /// <summary>
        /// Stores the given expression in a local variable and saves it into the resolution context for further reuse.
        /// </summary>
        /// <param name="expression">The expression to store.</param>
        /// <param name="resolutionContext">The resolution context.</param>
        /// <param name="resolveType">The resolve type.</param>
        /// <returns>The local variable.</returns>
        public Expression StoreExpressionIntoLocalVariable(Expression expression, ResolutionContext resolutionContext, Type resolveType)
        {
            var variable = resolveType.AsVariable();
            resolutionContext.AddDefinedVariable(this.ScopeId, variable);
            resolutionContext.AddInstruction(variable.AssignTo(expression));
            return variable;
        }
    }
}