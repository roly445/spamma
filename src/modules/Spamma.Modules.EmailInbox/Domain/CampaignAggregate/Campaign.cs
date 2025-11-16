using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate;

/// <summary>
/// Business logic for Campaign aggregate.
/// </summary>
internal partial class Campaign : AggregateRoot
{
    private DateTime? _deletedAt;

    private Campaign()
    {
    }

    public override Guid Id { get; protected set; }

    internal Guid DomainId { get; private set; }

    internal Guid SubdomainId { get; private set; }

    internal string CampaignValue { get; private set; } = string.Empty;

    internal Guid? SampleMessageId { get; private set; }

    internal DateTime CreatedAt { get; private set; }

    internal DateTime DeletedAt => this._deletedAt ?? throw new InvalidOperationException("Campaign is not deleted. Check IsDeleted before accessing DeletedAt.");

    internal bool IsDeleted => this._deletedAt.HasValue;

    internal int TotalCaptures { get; private set; }

    internal DateTimeOffset LastCapturedAt { get; private set; }

    internal static Result<Campaign, BluQubeErrorData> Create(
        Guid campaignId,
        Guid domainId,
        Guid subdomainId,
        string campaignValue,
        Guid messageId,
        DateTime createdAt,
        DateTimeOffset receivedAt)
    {
        if (domainId == Guid.Empty)
        {
            return Result.Fail<Campaign, BluQubeErrorData>(
                new BluQubeErrorData(
                    EmailInboxErrorCodes.InvalidCampaignData,
                    "DomainId cannot be empty."));
        }

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
        var @event = new CampaignCreated(campaignId, domainId, subdomainId, campaignValue, messageId, createdAt, receivedAt);
        campaign.RaiseEvent(@event);

        return Result.Ok<Campaign, BluQubeErrorData>(campaign);
    }

    internal ResultWithError<BluQubeErrorData> RecordCapture(Guid messageId, DateTimeOffset capturedAt)
    {
        if (this.IsDeleted)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                EmailInboxErrorCodes.CampaignAlreadyDeleted,
                $"Campaign '{this.Id}' has already been deleted."));
        }

        if (messageId == Guid.Empty)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                EmailInboxErrorCodes.InvalidCampaignData,
                "MessageId cannot be empty."));
        }

        var @event = new CampaignCaptured(capturedAt);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Delete(DateTime deletedAt)
    {
        if (this.IsDeleted)
        {
            return ResultWithError.Fail(new BluQubeErrorData(
                EmailInboxErrorCodes.CampaignAlreadyDeleted,
                $"Campaign '{this.Id}' has already been deleted."));
        }

        var @event = new CampaignDeleted(deletedAt);
        this.RaiseEvent(@event);

        return ResultWithError.Ok<BluQubeErrorData>();
    }
}