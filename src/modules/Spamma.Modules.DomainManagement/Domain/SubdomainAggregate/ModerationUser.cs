using ResultMonad;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

internal class ModerationUser
{
    private ModerationUser(Guid userId, DateTime addedAt)
    {
        this.UserId = userId;
        this.AddedAt = addedAt;
    }

    internal Guid UserId { get; private set; }

    internal DateTime AddedAt { get; private set; }

    internal DateTime? RemovedAt { get; private set; }

    internal static ModerationUser Create(Guid userId, DateTime addedAt)
    {
        return new ModerationUser(userId, addedAt);
    }

    internal Result Remove(DateTime removedAt)
    {
        if (this.RemovedAt.HasValue)
        {
            return Result.Fail();
        }

        this.RemovedAt = removedAt;
        return Result.Ok();
    }
}