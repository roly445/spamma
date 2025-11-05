using BluQube.Queries;
using Marten;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

public class GetCampaignDetailQueryProcessor(IDocumentSession session) : IQueryProcessor<GetCampaignDetailQuery, GetCampaignDetailQueryResult>
{
    public async Task<QueryResult<GetCampaignDetailQueryResult>> Handle(GetCampaignDetailQuery request, CancellationToken cancellationToken)
    {
        // Get the main campaign summary (by CampaignId only, we'll check authorization after)
        var campaign = await session.Query<CampaignSummary>()
            .FirstOrDefaultAsync(c => c.CampaignId == request.CampaignId, cancellationToken);

        if (campaign == null)
        {
            return QueryResult<GetCampaignDetailQueryResult>.Failed();
        }

        // If SubdomainId was provided, verify authorization
        if (request.SubdomainId != Guid.Empty && campaign.SubdomainId != request.SubdomainId)
        {
            return QueryResult<GetCampaignDetailQueryResult>.Failed();
        }

        // Get the sample message ID from campaign (set on creation to the first message)
        var sampleMessageId = campaign.SampleMessageId;

        GetCampaignDetailQueryResult.SampleMessage? sampleData = null;
        if (sampleMessageId.HasValue)
        {
            var email = await session.Query<EmailLookup>()
                .FirstOrDefaultAsync(e => e.Id == sampleMessageId.Value, cancellationToken);

            if (email != null)
            {
                var from = string.Join(", ", email.EmailAddresses.Where(ea => ea.EmailAddressType == 0).Select(ea => ea.Address));
                var to = string.Join(", ", email.EmailAddresses.Where(ea => ea.EmailAddressType != 0).Select(ea => ea.Address));

                sampleData = new GetCampaignDetailQueryResult.SampleMessage(
                    email.Id,
                    email.Subject,
                    from,
                    to,
                    email.WhenSent,
                    email.Subject);
            }
        }

        // Build time buckets based on actual campaign data
        var timeBuckets = new List<GetCampaignDetailQueryResult.TimeBucket>();

        // Create a single bucket spanning the campaign's lifetime
        timeBuckets.Add(new GetCampaignDetailQueryResult.TimeBucket(
            campaign.FirstReceivedAt,
            campaign.LastReceivedAt,
            campaign.TotalCaptured));

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
