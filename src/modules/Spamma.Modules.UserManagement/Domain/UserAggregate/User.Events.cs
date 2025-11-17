using Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate;

/// <summary>
/// Event handling for the User aggregate.
/// </summary>
public partial class User
{
    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case UserCreated createdEvent:
                this.Apply(createdEvent);
                break;
            case AuthenticationStarted startedEvent:
                this.Apply(startedEvent);
                break;
            case AuthenticationCompleted completedEvent:
                this.Apply(completedEvent);
                break;
            case AuthenticationFailed failedEvent:
                this.Apply(failedEvent);
                break;
            case AccountSuspended suspendedEvent:
                this.Apply(suspendedEvent);
                break;
            case AccountUnsuspended unSuspendedEvent:
                this.Apply(unSuspendedEvent);
                break;
            case DetailsChanged detailsChangedEvent:
                this.Apply(detailsChangedEvent);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void Apply(UserCreated created)
    {
        this.Id = created.UserId;
        this.Name = created.Name;
        this.SecurityStamp = created.SecurityStamp;
        this.EmailAddress = created.EmailAddress;
        this.SystemRole = created.SystemRole;
    }

    private void Apply(AuthenticationStarted @event)
    {
        var authenticationAttempt = new AuthenticationAttempt(@event.AuthenticationAttemptId, @event.StartedAt);
        this._authenticationAttempts.Add(authenticationAttempt);
    }

    private void Apply(AuthenticationCompleted @event)
    {
        var authenticationAttempt = this._authenticationAttempts.Single(a => a.Id == @event.AuthenticationAttemptId);
        authenticationAttempt.Complete(@event.CompletedAt);
        this.SecurityStamp = @event.SecurityStamp;
    }

    private void Apply(AuthenticationFailed @event)
    {
        var authenticationAttempt = this._authenticationAttempts.Single(a => a.Id == @event.AuthenticationAttemptId);
        authenticationAttempt.Fail(@event.FailedAt);
        this.SecurityStamp = @event.SecurityStamp;
    }

    private void Apply(AccountSuspended @event)
    {
        this._accountSuspensionAudits.Add(AccountSuspensionAudit.CreateSuspension(@event.SuspendedAt, @event.Reason, @event.Notes));
        this.SecurityStamp = @event.SecurityStamp;
        this.IsSuspended = true;
    }

    private void Apply(AccountUnsuspended @event)
    {
        this._accountSuspensionAudits.Add(AccountSuspensionAudit.CreateUnsuspension(@event.SuspendedAt));
        this.SecurityStamp = @event.SecurityStamp;
        this.IsSuspended = false;
    }

    private void Apply(DetailsChanged @event)
    {
        this.EmailAddress = @event.EmailAddress;
        this.Name = @event.Name;
        this.SystemRole = @event.SystemRole;
    }
}