using JasperFx.Events;
using Marten.Events.Aggregation;
using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.Infrastructure;

public sealed class AggregateReplayProjection<T>(Func<T?, object, T> replay) : SingleStreamProjection<T, Guid>
    where T : AggregateRoot
{
    public override T Evolve(T? snapshot, Guid id, IEvent e)
    {
        return replay(snapshot, e.Data);
    }
}
