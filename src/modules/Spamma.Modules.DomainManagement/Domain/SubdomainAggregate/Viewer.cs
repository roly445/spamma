using ResultMonad;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

public class Viewer
{
    private Viewer(Guid userId, DateTime addedAt)
    {
        this.UserId = userId;
        this.AddedAt = addedAt;
    }

    public Guid UserId { get; private set; }

    internal DateTime AddedAt { get; private set; }

    internal DateTime? RemovedAt { get; private set; }

    internal static Viewer Create(Guid userId, DateTime whenAdded)
    {
        return new Viewer(userId, whenAdded);
    }

    internal Result Remove(DateTime whenRemoved)
    {
        if (this.RemovedAt.HasValue)
        {
            return Result.Fail();
        }

        this.RemovedAt = whenRemoved;
        return Result.Ok();
    }
}