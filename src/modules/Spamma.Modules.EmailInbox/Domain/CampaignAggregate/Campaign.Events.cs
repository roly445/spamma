namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate;

/// <summary>
/// Event handling for the Campaign aggregate.
/// </summary>
internal partial class Campaign
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
        this.DomainId = @event.DomainId;
        this.SubdomainId = @event.SubdomainId;
        this.CampaignValue = @event.CampaignValue;
        this.CreatedAt = @event.CreatedAt;
        this.SampleMessageId = @event.MessageId;
        this.LastCapturedAt = @event.ReceivedAt;
    }

    private void Apply(Events.CampaignCaptured @event)
    {
        this.TotalCaptures++;
        this.LastCapturedAt = @event.CapturedAt;
    }

    private void Apply(Events.CampaignDeleted @event)
    {
        this._deletedAt = @event.DeletedAt;
    }
}