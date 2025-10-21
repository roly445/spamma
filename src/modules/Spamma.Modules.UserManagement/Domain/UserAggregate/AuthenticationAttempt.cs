namespace Spamma.Modules.UserManagement.Domain.UserAggregate;

public class AuthenticationAttempt
{
    internal AuthenticationAttempt(Guid id, DateTime whenStarted)
    {
        this.Id = id;
        this.WhenStarted = whenStarted;
    }

    public Guid Id { get; private set; }

    public DateTime WhenStarted { get; private set; }

    public DateTime? WhenCompleted { get; private set; }

    public DateTime? WhenFailed { get; private set; }

    internal void Complete(DateTime whenCompleted)
    {
        this.WhenCompleted = whenCompleted;
    }

    internal void Fail(DateTime whenFailed)
    {
        this.WhenFailed = whenFailed;
    }
}