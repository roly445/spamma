using Marten;
using MaybeMonad;
using ResultMonad;
using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.Infrastructure;

public class GenericRepository<T>(IDocumentSession session) : IRepository<T>
    where T : AggregateRoot
{
    public async Task<Maybe<T>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Use Marten's event sourcing to reconstruct the aggregate from events
        var aggregate = await session.Events.AggregateStreamAsync<T>(id, token: ct);
        return Maybe.From(aggregate!);
    }

    public async Task<Result> SaveAsync(T aggregate, CancellationToken ct = default)
    {
        // Get any uncommitted events from the aggregate
        var uncommittedEvents = aggregate.GetUncommittedEvents().ToArray();

        if (uncommittedEvents.Any())
        {
            // Append the events to the aggregate's event stream
            session.Events.Append(aggregate.Id, uncommittedEvents);

            // Mark the events as committed so they won't be saved again
            aggregate.MarkEventsAsCommitted();
        }

        await session.SaveChangesAsync(ct);
        return Result.Ok();
    }
}