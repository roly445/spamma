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
        var aggregate = await session.Events.AggregateStreamAsync<T>(id, token: ct);
        return Maybe.From(aggregate!);
    }

    public async Task<Result> SaveAsync(T aggregate, CancellationToken ct = default)
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents().ToArray();

        if (uncommittedEvents.Any())
        {
            session.Events.Append(aggregate.Id, uncommittedEvents);

            aggregate.MarkEventsAsCommitted();
        }

        await session.SaveChangesAsync(ct);
        return Result.Ok();
    }
}