using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate;

/// <summary>
/// Event handling for the Email aggregate.
/// </summary>
public partial class Email
{
    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case EmailReceived emailReceived:
                this.Apply(emailReceived);
                break;
            case EmailDeleted emailDeleted:
                this.Apply(emailDeleted);
                break;
            case EmailMarkedAsFavorite emailMarkedAsFavorite:
                this.Apply(emailMarkedAsFavorite);
                break;
            case EmailUnmarkedAsFavorite emailUnmarkedAsFavorite:
                this.Apply(emailUnmarkedAsFavorite);
                break;
            case CampaignCaptured campaignCaptured:
                this.Apply(campaignCaptured);
                break;
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void Apply(CampaignCaptured @event)
    {
       this._campaignId = @event.CampaignId;
    }

    private void Apply(EmailDeleted @event)
    {
        this._deletedAt = @event.DeletedAt;
    }

    private void Apply(EmailReceived @event)
    {
        this.Id = @event.EmailId;
        this.DomainId = @event.DomainId;
        this.SubdomainId = @event.SubdomainId;
        this.Subject = @event.Subject;
        this.WhenSent = @event.SentAt;
        this._emailAddresses.AddRange(@event.EmailAddresses.Select(ea => new EmailAddress(ea.Address, ea.Name, ea.EmailAddressType)));
    }

    private void Apply(EmailMarkedAsFavorite unused)
    {
        _ = unused;
        this.IsFavorite = true;
    }

    private void Apply(EmailUnmarkedAsFavorite unused)
    {
        _ = unused;
        this.IsFavorite = false;
    }
}