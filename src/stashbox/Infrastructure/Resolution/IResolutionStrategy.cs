﻿using Stashbox.Entity;
using Stashbox.Entity.Resolution;
using System.Linq.Expressions;

namespace Stashbox.Infrastructure.Resolution
{
    /// <summary>
    /// Represents a resolution strategy.
    /// </summary>
    public interface IResolutionStrategy
    {
        /// <summary>
        /// Builds a resolution expression for a dependency.
        /// </summary>
        /// <param name="containerContext">The <see cref="IContainerContext"/> of the <see cref="StashboxContainer"/></param>
        /// <param name="resolutionInfo">The resolution info.</param>
        /// <param name="typeInformation">The type info of the requested service.</param>
        /// <param name="injectionParameters">The injection parameters.</param>
        /// <returns>The created resolution target.</returns>
        Expression BuildResolutionExpression(IContainerContext containerContext, ResolutionInfo resolutionInfo, TypeInformation typeInformation,
            InjectionParameter[] injectionParameters);

        /// <summary>
        /// Builds resolution expressions for an enumerable dependency.
        /// </summary>
        /// <param name="containerContext">The <see cref="IContainerContext"/> of the <see cref="StashboxContainer"/></param>
        /// <param name="resolutionInfo">The resolution info.</param>
        /// <param name="typeInformation">The type info of the requested service.</param>
        /// <returns>The created resolution target.</returns>
        Expression[] BuildResolutionExpressions(IContainerContext containerContext, ResolutionInfo resolutionInfo,
            TypeInformation typeInformation);
    }
}
