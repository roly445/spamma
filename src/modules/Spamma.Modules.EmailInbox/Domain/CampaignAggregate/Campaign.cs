using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate;

/// <summary>
/// Represents a campaign aggregate root for tracking email campaigns and capture events.
/// </summary>
public partial class Campaign : AggregateRoot
{
    private Campaign()
    {
    }

    public override Guid Id { get; protected set; }

    internal Guid SubdomainId { get; private set; }

    internal string CampaignValue { get; private set; } = string.Empty;

    internal Guid? SampleMessageId { get; private set; }

    internal DateTimeOffset CreatedAt { get; private set; }

    internal DateTimeOffset? DeletedAt { get; private set; }

    internal List<Guid> MessageIds { get; private set; } = new();

    internal static Result<Campaign, BluQubeErrorData> Create(
        Guid campaignId,
        Guid subdomainId,
        string campaignValue,
        Guid messageId,
        DateTimeOffset createdAt)
    {
        if (subdomainId == Guid.Empty)
        {
            return Result.Fail<Campaign, BluQubeErrorData>(
                new BluQubeErrorData(
                    EmailInboxErrorCodes.InvalidCampaignData,
                    "SubdomainId cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(campaignValue))
        {
            return Result.Fail<Campaign, BluQubeErrorData>(
                new BluQubeErrorData(
                    EmailInboxErrorCodes.InvalidCampaignData,
                    "CampaignValue cannot be empty."));
        }

        if (campaignValue.Length > 255)
        {
            return Result.Fail<Campaign, BluQubeErrorData>(
                new BluQubeErrorData(
                    EmailInboxErrorCodes.InvalidCampaignData,
                    "CampaignValue must not exceed 255 characters."));
        }

        if (messageId == Guid.Empty)
        {
            return Result.Fail<Campaign, BluQubeErrorData>(
                new BluQubeErrorData(
                    EmailInboxErrorCodes.InvalidCampaignData,
                    "MessageId cannot be empty."));
        }

        var campaign = new Campaign();
        var @event = new CampaignCreated(campaignId, subdomainId, campaignValue, messageId, createdAt);
        campaign.RaiseEvent(@event);

        return Result.Ok<Campaign, BluQubeErrorData>(campaign);
    }

    internal ResultWithError<BluQubeErrorData> RecordCapture(Guid messageId, DateTimeOffset capturedAt)
    {
        if (this.DeletedAt.HasValue)
        {
            return ResultWithError.Fail<BluQubeErrorData>(new BluQubeErrorData(
                EmailInboxErrorCodes.CampaignAlreadyDeleted,
                $"Campaign '{this.Id}' has already been deleted."));
        }

        if (messageId == Guid.Empty)
        {
            return ResultWithError.Fail<BluQubeErrorData>(new BluQubeErrorData(
                EmailInboxErrorCodes.InvalidCampaignData,
                "MessageId cannot be empty."));
        }

        var @event = new CampaignCaptured(this.Id, messageId, capturedAt);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Delete(DateTimeOffset deletedAt, bool force = false)
    {
        if (this.DeletedAt.HasValue)
        {
            return ResultWithError.Fail<BluQubeErrorData>(new BluQubeErrorData(
                EmailInboxErrorCodes.CampaignAlreadyDeleted,
                $"Campaign '{this.Id}' has already been deleted."));
        }

        var @event = new CampaignDeleted(this.Id, deletedAt, force);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }
}
