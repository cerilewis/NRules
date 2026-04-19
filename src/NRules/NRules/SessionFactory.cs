using System;
using System.Collections.Generic;
using NRules.AgendaFilters;
using NRules.Diagnostics;
using NRules.Extensibility;
using NRules.Rete;

namespace NRules;

/// <summary>
/// Base interface for session factories, providing common configuration shared
/// by both synchronous and asynchronous session factories.
/// </summary>
/// <seealso cref="ISessionFactory"/>
/// <seealso cref="IAsyncSessionFactory"/>
/// <seealso cref="RuleCompiler"/>
/// <threadsafety instance="true" />
public interface ISessionFactoryBase : ISessionSchemaProvider
{
    /// <summary>
    /// Provider of events aggregated across all rule sessions. 
    /// Event sender is used to convey the session instance responsible for the event.
    /// Use it to subscribe to various rules engine lifecycle events.
    /// </summary>
    IEventProvider Events { get; }

    /// <summary>
    /// Rules dependency resolver for all rules sessions.
    /// </summary>
    IDependencyResolver DependencyResolver { get; set; }

    /// <summary>
    /// Action interceptor for all rules sessions.
    /// If provided, invocation of rule actions is delegated to the interceptor.
    /// </summary>
    IActionInterceptor? ActionInterceptor { get; set; }
}

/// <summary>
/// Represents compiled production rules that can be used to create synchronous rules sessions.
/// Created by <see cref="RuleCompiler"/> by compiling rule model into an executable form.
/// </summary>
/// <remarks>
/// Session factory is expensive to create (because rules need to be compiled into an executable form).
/// Therefore there needs to be only a single instance of session factory for a given set of rules for the lifetime of the application.
/// If repeatedly running rules for different sets of facts, don't create a new session factory for each rules run.
/// Instead, have a single session factory and create a new rules session for each independent universe of facts.
/// </remarks>
/// <seealso cref="ISession"/>
/// <seealso cref="RuleCompiler"/>
/// <threadsafety instance="true" />
public interface ISessionFactory : ISessionFactoryBase
{
    /// <summary>
    /// Creates a new synchronous rules session.
    /// </summary>
    /// <returns>New rules session.</returns>
    ISession CreateSession();

    /// <summary>
    /// Creates a new synchronous rules session.
    /// </summary>
    /// <param name="initializationAction">Action invoked on the newly created session, before the session is activated (which could result in rule matches placed on the agenda).</param>
    /// <returns>New rules session.</returns>
    ISession CreateSession(Action<ISession> initializationAction);
}

/// <summary>
/// Represents compiled production rules that can be used to create asynchronous rules sessions.
/// Created by <see cref="RuleCompiler"/> by compiling rule model into an executable form.
/// </summary>
/// <remarks>
/// Session factory is expensive to create (because rules need to be compiled into an executable form).
/// Therefore there needs to be only a single instance of session factory for a given set of rules for the lifetime of the application.
/// If repeatedly running rules for different sets of facts, don't create a new session factory for each rules run.
/// Instead, have a single session factory and create a new rules session for each independent universe of facts.
/// </remarks>
/// <seealso cref="IAsyncSession"/>
/// <seealso cref="RuleCompiler"/>
/// <threadsafety instance="true" />
public interface IAsyncSessionFactory : ISessionFactoryBase
{
    /// <summary>
    /// Async action interceptor for all rules sessions.
    /// If provided, invocation of async rule actions is delegated to the interceptor.
    /// </summary>
    IAsyncActionInterceptor? AsyncActionInterceptor { get; set; }

    /// <summary>
    /// Creates a new asynchronous rules session.
    /// </summary>
    /// <returns>New async rules session.</returns>
    IAsyncSession CreateSession();

    /// <summary>
    /// Creates a new asynchronous rules session.
    /// </summary>
    /// <param name="initializationAction">Action invoked on the newly created session, before the session is activated (which could result in rule matches placed on the agenda).</param>
    /// <returns>New async rules session.</returns>
    IAsyncSession CreateSession(Action<IAsyncSession> initializationAction);
}

internal abstract class SessionFactoryBase
{
    protected readonly INetwork _network;
    private readonly IFactIdentityComparer _factIdentityComparer;
    private readonly List<ICompiledRule> _compiledRules;
    private readonly IEventAggregator _eventAggregator = new EventAggregator();

    protected SessionFactoryBase(INetwork network, IEnumerable<ICompiledRule> compiledRules,
        IFactIdentityComparer factIdentityComparer)
    {
        _network = network;
        _factIdentityComparer = factIdentityComparer;
        _compiledRules = new List<ICompiledRule>(compiledRules);
        DependencyResolver = new DependencyResolver();
    }

    public IEventProvider Events => _eventAggregator;
    public IDependencyResolver DependencyResolver { get; set; }
    public IActionInterceptor? ActionInterceptor { get; set; }

    protected (IAgendaInternal agenda, IWorkingMemory workingMemory, IEventAggregator eventAggregator,
        IMetricsAggregator metricsAggregator, IIdGenerator idGenerator) CreateSessionComponents()
    {
        var agenda = CreateAgenda();
        var workingMemory = new WorkingMemory(_factIdentityComparer);
        var eventAggregator = new EventAggregator(_eventAggregator);
        var metricsAggregator = new MetricsAggregator();
        var idGenerator = new IdGenerator();
        return (agenda, workingMemory, eventAggregator, metricsAggregator, idGenerator);
    }

    private IAgendaInternal CreateAgenda()
    {
        var agenda = new Agenda();
        foreach (var compiledRule in _compiledRules)
        {
            var ruleFilters = CreateRuleFilters(compiledRule);
            foreach (var filter in ruleFilters)
            {
                agenda.AddFilter(compiledRule.Definition, filter);
            }
        }
        return agenda;
    }

    private static IEnumerable<IAgendaFilter> CreateRuleFilters(ICompiledRule compiledRule)
    {
        var filterConditions = compiledRule.Filter.Conditions;
        if (filterConditions.Count > 0)
        {
            var filter = new PredicateAgendaFilter(filterConditions);
            yield return filter;
        }
        var filterKeySelectors = compiledRule.Filter.KeySelectors;
        if (filterKeySelectors.Count > 0)
        {
            var filter = new KeyChangeAgendaFilter(filterKeySelectors);
            yield return filter;
        }
    }

    protected ReteGraph GetSchema() => _network.GetSchema();
}

internal sealed class SessionFactory : SessionFactoryBase, ISessionFactory
{
    public SessionFactory(INetwork network, IEnumerable<ICompiledRule> compiledRules,
        IFactIdentityComparer factIdentityComparer)
        : base(network, compiledRules, factIdentityComparer)
    {
    }

    public ISession CreateSession()
    {
        return CreateSession(null);
    }

    public ISession CreateSession(Action<ISession>? initializationAction)
    {
        var (agenda, workingMemory, eventAggregator, metricsAggregator, idGenerator) = CreateSessionComponents();
        var actionExecutor = new ActionExecutor();
        var session = new Session(_network, agenda, workingMemory, eventAggregator, metricsAggregator, actionExecutor, idGenerator, DependencyResolver, ActionInterceptor);
        initializationAction?.Invoke(session);
        session.Activate();
        return session;
    }

    ReteGraph ISessionSchemaProvider.GetSchema() => GetSchema();
}

internal sealed class AsyncSessionFactory : SessionFactoryBase, IAsyncSessionFactory
{
    public AsyncSessionFactory(INetwork network, IEnumerable<ICompiledRule> compiledRules,
        IFactIdentityComparer factIdentityComparer)
        : base(network, compiledRules, factIdentityComparer)
    {
    }

    public IAsyncActionInterceptor? AsyncActionInterceptor { get; set; }

    public IAsyncSession CreateSession()
    {
        return CreateSession(null);
    }

    public IAsyncSession CreateSession(Action<IAsyncSession>? initializationAction)
    {
        var (agenda, workingMemory, eventAggregator, metricsAggregator, idGenerator) = CreateSessionComponents();
        var actionExecutor = new ActionExecutor();
        var session = new AsyncSession(_network, agenda, workingMemory, eventAggregator, metricsAggregator, actionExecutor, idGenerator, DependencyResolver, ActionInterceptor, AsyncActionInterceptor);
        initializationAction?.Invoke(session);
        session.Activate();
        return session;
    }

    ReteGraph ISessionSchemaProvider.GetSchema() => GetSchema();
}
