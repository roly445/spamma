using Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate;

public partial class Domain
{
    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case DomainCreated domainCreated:
                this.Apply(domainCreated);
                break;
            case DomainVerified domainVerified:
                this.Apply(domainVerified);
                break;
            case DetailsUpdated detailsUpdated:
                this.Apply(detailsUpdated);
                break;
            case DomainSuspended domainSuspended:
                this.Apply(domainSuspended);
                break;
            case DomainUnsuspended domainUnsuspended:
                this.Apply(domainUnsuspended);
                break;
            case ModerationUserAdded moderationUserAdded:
                this.Apply(moderationUserAdded);
                break;
            case ModerationUserRemoved moderationUserRemoved:
                this.Apply(moderationUserRemoved);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void Apply(ModerationUserRemoved @event)
    {
        this._moderationUsers.First(x => x.UserId == @event.UserId && !x.WhenRemoved.HasValue)
            .Remove(@event.WhenRemoved);
    }

    private void Apply(ModerationUserAdded @event)
    {
        this._moderationUsers.Add(ModerationUser.Create(@event.UserId, @event.WhenAdded));
    }

    private void Apply(DomainCreated @event)
    {
        this.Id = @event.DomainId;
        this.Name = @event.Name;
        this.PrimaryContactEmail = @event.PrimaryContactEmail;
        this.Description = @event.Description;
        this.VerificationToken = @event.VerificationToken;
        this.WhenCreated = @event.WhenCreated;
    }

    private void Apply(DomainVerified @event)
    {
        this.WhenVerified = @event.WhenVerified;
    }

    private void Apply(DetailsUpdated @event)
    {
        this.Description = @event.Description;
        this.PrimaryContactEmail = @event.PrimaryContactEmail;
    }

    private void Apply(DomainSuspended @event)
    {
        this._suspensionAudits.Add(DomainSuspensionAudit.CreateSuspension(@event.WhenSuspended, @event.Reason, @event.Notes));
    }

    private void Apply(DomainUnsuspended @event)
    {
        this._suspensionAudits.Add(DomainSuspensionAudit.CreateUnsuspension(@event.WhenUnsuspended));
    }
}