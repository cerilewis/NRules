using System.Collections.Generic;
using System.Threading.Tasks;
using NRules.RuleModel;

namespace NRules.Extensibility;

/// <summary>
/// Represents invocation of the proxied rule action.
/// </summary>
public interface IActionInvocation
{
    /// <summary>
    /// Action arguments.
    /// To get more information about the matched facts, whether they are passed to a given action or not,
    /// use <see cref="IContext"/> passed to the <see cref="IActionInterceptor.Intercept"/> method.
    /// </summary>
    /// <remarks>Action arguments also include dependencies that are passed to the action method.</remarks>
    /// <remarks>Action arguments don't include <c>IContext</c>.</remarks>
    IReadOnlyList<object?> Arguments { get; }

    /// <summary>
    /// Invokes the action synchronously.
    /// For async actions, this blocks until the action completes.
    /// </summary>
    void Invoke();

    /// <summary>
    /// Invokes the action asynchronously.
    /// For sync actions, this completes synchronously and returns a completed task.
    /// </summary>
    Task InvokeAsync();

    /// <summary>
    /// Activation events that trigger this action.
    /// </summary>
    ActionTrigger Trigger { get; }
}