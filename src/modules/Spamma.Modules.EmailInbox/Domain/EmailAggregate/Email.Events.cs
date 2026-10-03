using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate;

/// <summary>
/// Event handling for the Email aggregate.
/// </summary>
public partial class Email
{
    public static Email Create(EmailReceived @event)
    {
        var email = new Email();
        email.Apply(@event);
        return email;
    }

    public void Apply(CampaignCaptured @event)
    {
       this._campaignId = @event.CampaignId;
    }

    public void Apply(EmailDeleted @event)
    {
        this._deletedAt = @event.DeletedAt;
    }

    public void Apply(EmailReceived @event)
    {
        this.Id = @event.EmailId;
        this.DomainId = @event.DomainId;
        this.SubdomainId = @event.SubdomainId;
        this.Subject = @event.Subject;
        this.WhenSent = @event.SentAt;
        this._emailAddresses.AddRange(@event.EmailAddresses.Select(ea => new EmailAddress(ea.Address, ea.Name, ea.EmailAddressType)));
    }

    public void Apply(EmailMarkedAsFavorite unused)
    {
        _ = unused;
        this.IsFavorite = true;
    }

    public void Apply(EmailUnmarkedAsFavorite unused)
    {
        _ = unused;
        this.IsFavorite = false;
    }

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
}
