using System.Collections.Generic;
using NRules.Diagnostics;

namespace NRules;

internal interface IExecutionContext
{
    ISessionInternalBase Session { get; }
    IWorkingMemory WorkingMemory { get; }
    IAgendaInternal Agenda { get; }
    IEventAggregator EventAggregator { get; }
    IMetricsAggregator MetricsAggregator { get; }
    IIdGenerator IdGenerator { get; }
    IFactIdentityComparer FactIdentityComparer { get; }
}

internal class ExecutionContext(
    ISessionInternalBase session,
    IWorkingMemory workingMemory,
    IAgendaInternal agenda,
    IEventAggregator eventAggregator,
    IMetricsAggregator metricsAggregator,
    IIdGenerator idGenerator)
    : IExecutionContext
{
    public ISessionInternalBase Session { get; } = session;
    public IWorkingMemory WorkingMemory { get; } = workingMemory;
    public IAgendaInternal Agenda { get; } = agenda;
    public IEventAggregator EventAggregator { get; } = eventAggregator;
    public IMetricsAggregator MetricsAggregator { get; } = metricsAggregator;
    public IIdGenerator IdGenerator { get; } = idGenerator;
    public IFactIdentityComparer FactIdentityComparer => WorkingMemory.FactIdentityComparer;
    public Queue<Activation> UnlinkQueue { get; } = new();
}
