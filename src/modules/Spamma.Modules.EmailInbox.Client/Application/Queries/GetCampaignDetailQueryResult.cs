using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record GetCampaignDetailQueryResult(
    Guid CampaignId,
    string CampaignValue,
    DateTimeOffset FirstReceivedAt,
    DateTimeOffset LastReceivedAt,
    int TotalCaptured,
    IReadOnlyList<GetCampaignDetailQueryResult.TimeBucket> TimeBuckets,
    GetCampaignDetailQueryResult.SampleMessage? Sample,
    int TemporaryFailureMessages = 0,
    int PermanentFailureMessages = 0,
    int FailureDeliveryAttempts = 0) : IQueryResult
{
    public record TimeBucket(
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        int Count);

    public record SampleMessage(
        Guid MessageId,
        string Subject,
        string From,
        string To,
        DateTimeOffset ReceivedAt,
        string ContentPreview);
}
