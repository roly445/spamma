using DotNetCore.CAP;

namespace Spamma.Modules.Common.Domain.Contracts;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event, CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent;
}

public class IntegrationEventPublisher(ICapPublisher capPublisher) : IIntegrationEventPublisher
{
    public Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event, CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
    {
        return capPublisher.PublishAsync(@event.EventName, @event, cancellationToken: cancellationToken);
    }
}