﻿using System.Runtime.ExceptionServices;

namespace Polly.CircuitBreaker;

internal static class CircuitBreakerEngine
{
    [DebuggerDisableUserUnhandledExceptions]
    internal static TResult Implementation<TResult>(
        Func<Context, CancellationToken, TResult> action,
        Context context,
        ExceptionPredicates shouldHandleExceptionPredicates,
        ResultPredicates<TResult> shouldHandleResultPredicates,
        ICircuitController<TResult> breakerController,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        breakerController.OnActionPreExecute();

        try
        {
            TResult result = action(context, cancellationToken);

            if (shouldHandleResultPredicates.AnyMatch(result))
            {
                breakerController.OnActionFailure(new DelegateResult<TResult>(result), context);
            }
            else
            {
                breakerController.OnActionSuccess(context);
            }

            return result;
        }
        catch (Exception ex)
        {
            Exception handledException = shouldHandleExceptionPredicates.FirstMatchOrDefault(ex);
            if (handledException == null)
            {
                throw;
            }

            breakerController.OnActionFailure(new DelegateResult<TResult>(handledException), context);

            if (handledException != ex)
            {
                ExceptionDispatchInfo.Capture(handledException).Throw();
            }

            throw;
        }
    }
}
