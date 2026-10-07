namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate;

/// <summary>
/// Event handling for the Campaign aggregate.
/// </summary>
public partial class Campaign
{
    internal static Campaign Replay(Campaign? aggregate, object @event)
    {
        if (aggregate is null && @event is not Events.CampaignCreated and not Events.CampaignObservedViaFailure)
        {
            throw new ArgumentException("The first event must create or observe a campaign.", nameof(@event));
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
            case Events.CampaignObservedViaFailure observed:
                this.Id = observed.CampaignId;
                this.DomainId = observed.DomainId;
                this.SubdomainId = observed.SubdomainId;
                this.CampaignValue = observed.CampaignValue;
                this.CreatedAt = observed.ObservedAt.UtcDateTime;
                break;
            case Events.CampaignFirstCaptured first:
                this.SampleMessageId = first.MessageId;
                this.LastCapturedAt = first.CapturedAt;
                break;
            case Events.CampaignDeliveryFailureRecorded failure:
                this._failureIds.Add(failure.FailureId);
                this._failureAttemptIds.Add(failure.AttemptId);
                break;
            case Events.CampaignDeliveryFailureRetried retry:
                this._failureAttemptIds.Add(retry.AttemptId);
                break;
            case Events.CampaignCaptured campaignCaptured:
                this.ApplyRecorded(campaignCaptured);
                break;
            case Events.CampaignCapturedV2 campaignCapturedV2:
                this.ApplyRecorded(campaignCapturedV2);
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

    private void ApplyRecorded(Events.CampaignCapturedV2 @event)
    {
        this.TotalCaptures++;
        this.LastCapturedAt = @event.CapturedAt;
        this._capturedMessageIds.Add(@event.MessageId);
    }

    private void ApplyRecorded(Events.CampaignDeleted @event)
    {
        this._deletedAt = @event.DeletedAt;
    }
}
