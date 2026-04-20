using NRules.RuleModel;

namespace NRules.Extensibility;

/// <summary>
/// Context for dependency resolution.
/// </summary>
public interface IResolutionContext
{
    /// <summary>
    /// Rules engine session that requested dependency resolution.
    /// </summary>
    ISessionBase Session { get; }

    /// <summary>
    /// Rule that requested dependency resolution.
    /// </summary>
    IRuleDefinition Rule { get; }
}

internal class ResolutionContext(ISessionBase session, IRuleDefinition rule) : IResolutionContext
{
    public ISessionBase Session { get; } = session;
    public IRuleDefinition Rule { get; } = rule;
}
