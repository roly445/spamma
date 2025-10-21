namespace Spamma.Modules.Common.Domain.Contracts;

public abstract class AggregateRoot
{
    private readonly List<object> _uncommittedEvents = new();

    public abstract Guid Id { get; protected set; }

    public IEnumerable<object> GetUncommittedEvents() => this._uncommittedEvents.AsReadOnly();

    public void MarkEventsAsCommitted() => this._uncommittedEvents.Clear();

    public void LoadFromHistory(IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            this.ApplyEvent(@event);
        }
    }

    protected void RaiseEvent(object @event)
    {
        this._uncommittedEvents.Add(@event);
        this.ApplyEvent(@event);
    }

    protected abstract void ApplyEvent(object @event);
}