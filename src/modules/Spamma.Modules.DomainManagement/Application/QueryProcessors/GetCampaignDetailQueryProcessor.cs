using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class GetCampaignDetailQueryProcessor(IDocumentSession session) : IQueryProcessor<GetCampaignDetailQuery, GetCampaignDetailQueryResult>
{
    public async Task<QueryResult<GetCampaignDetailQueryResult>> Handle(GetCampaignDetailQuery request, CancellationToken cancellationToken)
    {
        // Get the main campaign summary
        var campaign = await session.Query<CampaignSummary>()
            .FirstOrDefaultAsync(c => c.CampaignId == request.CampaignId && c.SubdomainId == request.SubdomainId, cancellationToken);

        if (campaign == null)
        {
            return QueryResult<GetCampaignDetailQueryResult>.Failed();
        }

        // Get the sample (if any)
        var sample = await session.Query<CampaignSample>()
            .FirstOrDefaultAsync(s => s.CampaignId == request.CampaignId, cancellationToken);

        var sampleData = sample != null
            ? new GetCampaignDetailQueryResult.SampleMessage(
                sample.MessageId,
                sample.Subject,
                sample.From,
                sample.To,
                sample.ReceivedAt,
                sample.ContentPreview)
            : null;

        // Build time buckets (simplified - would need event sourcing data in production)
        var timeBuckets = new List<GetCampaignDetailQueryResult.TimeBucket>();
        var bucketDays = request.DaysBucket ?? 7;
        var currentTime = campaign.LastReceivedAt;

        for (int i = 0; i < 4; i++)
        {
            var endTime = currentTime;
            var startTime = endTime.AddDays(-bucketDays);

            timeBuckets.Add(new GetCampaignDetailQueryResult.TimeBucket(
                startTime,
                endTime,
                Math.Max(1, campaign.TotalCaptured / 4)));

            currentTime = startTime;
        }

        var result = new GetCampaignDetailQueryResult(
            campaign.CampaignId,
            campaign.CampaignValue,
            campaign.FirstReceivedAt,
            campaign.LastReceivedAt,
            campaign.TotalCaptured,
            timeBuckets.OrderBy(t => t.StartTime).ToList(),
            sampleData);

        return QueryResult<GetCampaignDetailQueryResult>.Succeeded(result);
    }
}
