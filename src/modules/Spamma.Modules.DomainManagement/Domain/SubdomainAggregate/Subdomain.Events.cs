using Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

public partial class Subdomain
{
    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case SubdomainCreated subdomainCreated:
                this.Apply(subdomainCreated);
                break;
            case SubdomainUpdated detailsUpdated:
                this.Apply(detailsUpdated);
                break;
            case SubdomainSuspended subdomainSuspended:
                this.Apply(subdomainSuspended);
                break;
            case SubdomainUnsuspended subdomainUnsuspended:
                this.Apply(subdomainUnsuspended);
                break;
            case ModerationUserAdded moderationUserAdded:
                this.Apply(moderationUserAdded);
                break;
            case ModerationUserRemoved moderationUserRemoved:
                this.Apply(moderationUserRemoved);
                break;
            case ViewerAdded viewerAdded:
                this.Apply(viewerAdded);
                break;
            case ViewerRemoved viewerRemoved:
                this.Apply(viewerRemoved);
                break;
            case MxRecordChecked mxRecordChecked:
                this.Apply(mxRecordChecked);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void Apply(MxRecordChecked @event)
    {
        this._mxRecordChecks.Add(new MxRecordCheck(@event.WhenChecked, @event.MxStatus));
    }

    private void Apply(ViewerRemoved @event)
    {
        this._viewers.First(x => x.UserId == @event.UserId && !x.WhenRemoved.HasValue)
            .Remove(@event.WhenRemoved);
    }

    private void Apply(ViewerAdded @event)
    {
        this._viewers.Add(Viewer.Create(@event.UserId, @event.WhenAdded));
    }

    private void Apply(SubdomainUnsuspended @event)
    {
        this._suspensionAudits.Add(SubdomainSuspensionAudit.CreateUnsuspension(
            @event.WhenUnsuspended));
        this.IsSuspended = false;
    }

    private void Apply(SubdomainSuspended @event)
    {
        this._suspensionAudits.Add(SubdomainSuspensionAudit.CreateSuspension(
            @event.WhenSuspended,
            @event.Reason, @event.Notes));
        this.IsSuspended = true;
    }

    private void Apply(SubdomainUpdated @event)
    {
        this.Description = @event.Description;
    }

    private void Apply(SubdomainCreated @event)
    {
        this.Id = @event.SubdomainId;
        this.DomainId = @event.DomainId;
        this.Name = @event.Name;
        this.Description = @event.Description;
        this.WhenCreated = @event.WhenCreated;
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
}