namespace Spamma.Modules.UserManagement.Domain.UserAggregate;

public class AuthenticationAttempt
{
    private DateTime? _failedAt;
    private DateTime? _completedAt;

    internal AuthenticationAttempt(Guid id, DateTime startedAt)
    {
        this.Id = id;
        this.StartedAt = startedAt;
    }

    internal Guid Id { get; private set; }

    internal DateTime StartedAt { get; private set; }

    internal DateTime CompletedAt => this._completedAt ?? throw new InvalidOperationException("Authentication attempt has not been completed yet.");

    internal DateTime FailedAt => this._failedAt ?? throw new InvalidOperationException("Authentication attempt has not failed.");

    internal bool HasFinalized => this._completedAt.HasValue || this._failedAt.HasValue;

    internal void Complete(DateTime whenCompleted)
    {
        this._completedAt = whenCompleted;
    }

    internal void Fail(DateTime whenFailed)
    {
        this._failedAt = whenFailed;
    }
}