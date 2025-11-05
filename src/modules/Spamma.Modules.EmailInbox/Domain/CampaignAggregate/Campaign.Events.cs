namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate;

/// <summary>
/// Partial class containing event handling for Campaign aggregate.
/// </summary>
public partial class Campaign
{
    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case Events.CampaignCreated campaignCreated:
                this.Apply(campaignCreated);
                break;
            case Events.CampaignCaptured campaignCaptured:
                this.Apply(campaignCaptured);
                break;
            case Events.CampaignDeleted campaignDeleted:
                this.Apply(campaignDeleted);
                break;
        }
    }

    private void Apply(Events.CampaignCreated @event)
    {
        this.Id = @event.CampaignId;
        this.SubdomainId = @event.SubdomainId;
        this.CampaignValue = @event.CampaignValue;
        this.CreatedAt = @event.CreatedAt;
        this.SampleMessageId = @event.MessageId;
        this.MessageIds.Add(@event.MessageId);
    }

    private void Apply(Events.CampaignCaptured @event)
    {
        if (!this.MessageIds.Contains(@event.MessageId))
        {
            this.MessageIds.Add(@event.MessageId);
        }
    }

    private void Apply(Events.CampaignDeleted @event)
    {
        this.DeletedAt = @event.DeletedAt;
    }
}
