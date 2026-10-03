using Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate;

/// <summary>
/// Event handling for the User aggregate.
/// </summary>
public partial class User
{
    internal static User Replay(User? aggregate, object @event)
    {
        if (aggregate is null && @event is not UserCreated)
        {
            throw new ArgumentException("The first event must be UserCreated.", nameof(@event));
        }

        aggregate ??= new User();
        aggregate.ApplyEvent(@event);
        return aggregate;
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case UserCreated createdEvent:
                this.ApplyRecorded(createdEvent);
                break;
            case AuthenticationStarted startedEvent:
                this.ApplyRecorded(startedEvent);
                break;
            case AuthenticationCompleted completedEvent:
                this.ApplyRecorded(completedEvent);
                break;
            case AuthenticationFailed failedEvent:
                this.ApplyRecorded(failedEvent);
                break;
            case AccountSuspended suspendedEvent:
                this.ApplyRecorded(suspendedEvent);
                break;
            case AccountUnsuspended unSuspendedEvent:
                this.ApplyRecorded(unSuspendedEvent);
                break;
            case DetailsChanged detailsChangedEvent:
                this.ApplyRecorded(detailsChangedEvent);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void ApplyRecorded(UserCreated created)
    {
        this.Id = created.UserId;
        this.Name = created.Name;
        this.SecurityStamp = created.SecurityStamp;
        this.EmailAddress = created.EmailAddress;
        this.SystemRole = created.SystemRole;
    }

    private void ApplyRecorded(AuthenticationStarted @event)
    {
        var authenticationAttempt = new AuthenticationAttempt(@event.AuthenticationAttemptId, @event.StartedAt);
        this._authenticationAttempts.Add(authenticationAttempt);
    }

    private void ApplyRecorded(AuthenticationCompleted @event)
    {
        var authenticationAttempt = this._authenticationAttempts.Single(a => a.Id == @event.AuthenticationAttemptId);
        authenticationAttempt.Complete(@event.CompletedAt);
        this.SecurityStamp = @event.SecurityStamp;
    }

    private void ApplyRecorded(AuthenticationFailed @event)
    {
        var authenticationAttempt = this._authenticationAttempts.Single(a => a.Id == @event.AuthenticationAttemptId);
        authenticationAttempt.Fail(@event.FailedAt);
        this.SecurityStamp = @event.SecurityStamp;
    }

    private void ApplyRecorded(AccountSuspended @event)
    {
        this._accountSuspensionAudits.Add(AccountSuspensionAudit.CreateSuspension(@event.SuspendedAt, @event.Reason, @event.Notes));
        this.SecurityStamp = @event.SecurityStamp;
        this.IsSuspended = true;
    }

    private void ApplyRecorded(AccountUnsuspended @event)
    {
        this._accountSuspensionAudits.Add(AccountSuspensionAudit.CreateUnsuspension(@event.SuspendedAt));
        this.SecurityStamp = @event.SecurityStamp;
        this.IsSuspended = false;
    }

    private void ApplyRecorded(DetailsChanged @event)
    {
        this.EmailAddress = @event.EmailAddress;
        this.Name = @event.Name;
        this.SystemRole = @event.SystemRole;
    }
}
