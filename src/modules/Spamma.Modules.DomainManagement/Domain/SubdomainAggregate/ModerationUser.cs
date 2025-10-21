using ResultMonad;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

public class ModerationUser
{
    private ModerationUser(Guid userId, DateTime whenAdded)
    {
        this.UserId = userId;
        this.WhenAdded = whenAdded;
    }

    public Guid UserId { get; private set; }

    public DateTime WhenAdded { get; private set; }

    public DateTime? WhenRemoved { get; private set; }

    public static ModerationUser Create(Guid userId, DateTime whenAdded)
    {
        return new ModerationUser(userId, whenAdded);
    }

    public Result Remove(DateTime whenRemoved)
    {
        if (this.WhenRemoved.HasValue)
        {
            return Result.Fail();
        }

        this.WhenRemoved = whenRemoved;
        return Result.Ok();
    }
}