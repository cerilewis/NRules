using System.Collections.Generic;
using System.Threading.Tasks;
using NRules.RuleModel;

namespace NRules.Extensibility;

/// <summary>
/// Extension point for asynchronous rule action interception.
/// Implement both <see cref="IActionInterceptor"/> and <see cref="IAsyncActionInterceptor"/> on the same class
/// to support both synchronous (<see cref="ISession.Fire()"/>) and asynchronous
/// (<see cref="IAsyncSession.FireAsync()"/>) rule execution.
/// </summary>
/// <remarks>
/// When async actions are invoked via <c>IAsyncActionInterceptor</c>, exceptions thrown by actions
/// are not wrapped into <see cref="RuleRhsExpressionEvaluationException"/>. It is the responsibility
/// of the interceptor to handle the exceptions.
/// Exceptions thrown from the interceptor are not handled by the engine and just propagate up the call stack.
/// </remarks>
public interface IAsyncActionInterceptor
{
    /// <summary>
    /// Called by the rules engine in place of the action invocations when a rule fires asynchronously.
    /// The interceptor can add behavior to action invocation and choose to either proceed with the invocations or not.
    /// </summary>
    /// <param name="context">Action context, containing information about the firing rule and matched facts.</param>
    /// <param name="actions">Action invocations for rule actions being intercepted.</param>
    Task InterceptAsync(IContext context, IReadOnlyCollection<IActionInvocation> actions);
}
