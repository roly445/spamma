namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate;

/// <summary>
/// Event handling for the Campaign aggregate.
/// </summary>
public partial class Campaign
{
    internal static Campaign Replay(Campaign? aggregate, object @event)
    {
        if (aggregate is null && @event is not Events.CampaignCreated)
        {
            throw new ArgumentException("The first event must be CampaignCreated.", nameof(@event));
        }

        aggregate ??= new Campaign();
        aggregate.ApplyEvent(@event);
        return aggregate;
    }

    protected override void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case Events.CampaignCreated campaignCreated:
                this.ApplyRecorded(campaignCreated);
                break;
            case Events.CampaignCaptured campaignCaptured:
                this.ApplyRecorded(campaignCaptured);
                break;
            case Events.CampaignDeleted campaignDeleted:
                this.ApplyRecorded(campaignDeleted);
                break;
        }
    }

    private void ApplyRecorded(Events.CampaignCreated @event)
    {
        this.Id = @event.CampaignId;
        this.DomainId = @event.DomainId;
        this.SubdomainId = @event.SubdomainId;
        this.CampaignValue = @event.CampaignValue;
        this.CreatedAt = @event.CreatedAt;
        this.SampleMessageId = @event.MessageId;
        this.LastCapturedAt = @event.ReceivedAt;
    }

    private void ApplyRecorded(Events.CampaignCaptured @event)
    {
        this.TotalCaptures++;
        this.LastCapturedAt = @event.CapturedAt;
    }

    private void ApplyRecorded(Events.CampaignDeleted @event)
    {
        this._deletedAt = @event.DeletedAt;
    }
}
