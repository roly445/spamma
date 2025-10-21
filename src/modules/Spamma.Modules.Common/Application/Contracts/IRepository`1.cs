using MaybeMonad;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.Application.Contracts;

public interface IRepository<TAggregate>
    where TAggregate : AggregateRoot
{
    Task<Maybe<TAggregate>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result> SaveAsync(TAggregate aggregate, CancellationToken ct = default);
}