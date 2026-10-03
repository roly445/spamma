using Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate;

/// <summary>
/// Event handling for the Domain aggregate.
/// </summary>
public partial class Domain
{
    internal static Domain Replay(Domain? aggregate, object @event)
    {
        if (aggregate is null && @event is not DomainCreated)
        {
            throw new ArgumentException("The first event must be DomainCreated.", nameof(@event));
        }

        aggregate ??= new Domain();
        aggregate.ApplyEvent(@event);
        return aggregate;
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case DomainCreated domainCreated:
                this.ApplyRecorded(domainCreated);
                break;
            case DomainVerified domainVerified:
                this.ApplyRecorded(domainVerified);
                break;
            case DetailsUpdated detailsUpdated:
                this.ApplyRecorded(detailsUpdated);
                break;
            case DomainSuspended domainSuspended:
                this.ApplyRecorded(domainSuspended);
                break;
            case DomainUnsuspended domainUnsuspended:
                this.ApplyRecorded(domainUnsuspended);
                break;
            case ModerationUserAdded moderationUserAdded:
                this.ApplyRecorded(moderationUserAdded);
                break;
            case ModerationUserRemoved moderationUserRemoved:
                this.ApplyRecorded(moderationUserRemoved);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void ApplyRecorded(ModerationUserRemoved @event)
    {
        this._moderationUsers.First(x => x.UserId == @event.UserId && !x.RemovedAt.HasValue)
            .Remove(@event.RemovedAt);
    }

    private void ApplyRecorded(ModerationUserAdded @event)
    {
        this._moderationUsers.Add(ModerationUser.Create(@event.UserId, @event.AddedAt));
    }

    private void ApplyRecorded(DomainCreated @event)
    {
        this.Id = @event.DomainId;
        this.Name = @event.Name;
        this.PrimaryContactEmail = @event.PrimaryContactEmail;
        this.Description = @event.Description;
        this.VerificationToken = @event.VerificationToken;
        this.CreatedAt = @event.CreatedAt;
    }

    private void ApplyRecorded(DomainVerified @event)
    {
        this.VerifiedAt = @event.VerifiedAt;
    }

    private void ApplyRecorded(DetailsUpdated @event)
    {
        this.Description = @event.Description;
        this.PrimaryContactEmail = @event.PrimaryContactEmail;
    }

    private void ApplyRecorded(DomainSuspended @event)
    {
        this._suspensionAudits.Add(DomainSuspensionAudit.CreateSuspension(@event.SuspendedAt, @event.Reason, @event.Notes));
        this.IsSuspended = true;
    }

    private void ApplyRecorded(DomainUnsuspended @event)
    {
        this._suspensionAudits.Add(DomainSuspensionAudit.CreateUnsuspension(@event.UnsuspendedAt));
        this.IsSuspended = false;
    }
}
