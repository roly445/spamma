using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate;

/// <summary>
/// Event handling for the Email aggregate.
/// </summary>
public partial class Email
{
    internal static Email Replay(Email? aggregate, object @event)
    {
        if (aggregate is null && @event is not EmailReceived)
        {
            throw new ArgumentException("The first event must be EmailReceived.", nameof(@event));
        }

        aggregate ??= new Email();
        aggregate.ApplyEvent(@event);
        return aggregate;
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case EmailReceived emailReceived:
                this.ApplyRecorded(emailReceived);
                break;
            case EmailDeleted emailDeleted:
                this.ApplyRecorded(emailDeleted);
                break;
            case EmailMarkedAsFavorite emailMarkedAsFavorite:
                this.ApplyRecorded(emailMarkedAsFavorite);
                break;
            case EmailUnmarkedAsFavorite emailUnmarkedAsFavorite:
                this.ApplyRecorded(emailUnmarkedAsFavorite);
                break;
            case CampaignCaptured campaignCaptured:
                this.ApplyRecorded(campaignCaptured);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void ApplyRecorded(CampaignCaptured @event)
    {
       this._campaignId = @event.CampaignId;
    }

    private void ApplyRecorded(EmailDeleted @event)
    {
        this._deletedAt = @event.DeletedAt;
    }

    private void ApplyRecorded(EmailReceived @event)
    {
        this.Id = @event.EmailId;
        this.DomainId = @event.DomainId;
        this.SubdomainId = @event.SubdomainId;
        this.Subject = @event.Subject;
        this.WhenSent = @event.SentAt;
        this._emailAddresses.AddRange(@event.EmailAddresses.Select(ea => new EmailAddress(ea.Address, ea.Name, ea.EmailAddressType)));
    }

    private void ApplyRecorded(EmailMarkedAsFavorite unused)
    {
        _ = unused;
        this.IsFavorite = true;
    }

    private void ApplyRecorded(EmailUnmarkedAsFavorite unused)
    {
        _ = unused;
        this.IsFavorite = false;
    }
}
