using Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;

/// <summary>
/// Event handling for the Subdomain aggregate.
/// </summary>
public partial class Subdomain
{
    internal static Subdomain Replay(Subdomain? aggregate, object @event)
    {
        if (aggregate is null && @event is not SubdomainCreated)
        {
            throw new ArgumentException("The first event must be SubdomainCreated.", nameof(@event));
        }

        aggregate ??= new Subdomain();
        aggregate.ApplyEvent(@event);
        return aggregate;
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case SubdomainCreated subdomainCreated:
                this.ApplyRecorded(subdomainCreated);
                break;
            case SubdomainUpdated detailsUpdated:
                this.ApplyRecorded(detailsUpdated);
                break;
            case SubdomainSuspended subdomainSuspended:
                this.ApplyRecorded(subdomainSuspended);
                break;
            case SubdomainUnsuspended subdomainUnsuspended:
                this.ApplyRecorded(subdomainUnsuspended);
                break;
            case ModerationUserAdded moderationUserAdded:
                this.ApplyRecorded(moderationUserAdded);
                break;
            case ModerationUserRemoved moderationUserRemoved:
                this.ApplyRecorded(moderationUserRemoved);
                break;
            case ViewerAdded viewerAdded:
                this.ApplyRecorded(viewerAdded);
                break;
            case ViewerRemoved viewerRemoved:
                this.ApplyRecorded(viewerRemoved);
                break;
            case MxRecordChecked mxRecordChecked:
                this.ApplyRecorded(mxRecordChecked);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void ApplyRecorded(MxRecordChecked @event)
    {
        this._mxRecordChecks.Add(new MxRecordCheck(@event.LastCheckedAt, @event.MxStatus));
    }

    private void ApplyRecorded(ViewerRemoved @event)
    {
        this._viewers.First(x => x.UserId == @event.UserId && !x.RemovedAt.HasValue)
            .Remove(@event.RemovedAt);
    }

    private void ApplyRecorded(ViewerAdded @event)
    {
        this._viewers.Add(Viewer.Create(@event.UserId, @event.AddedAt));
    }

    private void ApplyRecorded(SubdomainUnsuspended @event)
    {
        this._suspensionAudits.Add(SubdomainSuspensionAudit.CreateUnsuspension(
            @event.UnsuspendedAt));
        this.IsSuspended = false;
    }

    private void ApplyRecorded(SubdomainSuspended @event)
    {
        this._suspensionAudits.Add(SubdomainSuspensionAudit.CreateSuspension(
            @event.SuspendedAt,
            @event.Reason, @event.Notes));
        this.IsSuspended = true;
    }

    private void ApplyRecorded(SubdomainUpdated @event)
    {
        this.Description = @event.Description;
    }

    private void ApplyRecorded(SubdomainCreated @event)
    {
        this.Id = @event.SubdomainId;
        this.DomainId = @event.DomainId;
        this.Name = @event.Name;
        this.Description = @event.Description;
        this.CreatedAt = @event.CreatedAt;
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
}
