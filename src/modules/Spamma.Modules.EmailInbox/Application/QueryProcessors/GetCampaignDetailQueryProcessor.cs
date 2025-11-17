using BluQube.Queries;
using Marten;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

internal class GetCampaignDetailQueryProcessor(IDocumentSession session) : IQueryProcessor<GetCampaignDetailQuery, GetCampaignDetailQueryResult>
{
    public async Task<QueryResult<GetCampaignDetailQueryResult>> Handle(GetCampaignDetailQuery request, CancellationToken cancellationToken)
    {
        var campaign = await session.Query<CampaignSummary>()
            .FirstOrDefaultAsync(c => c.CampaignId == request.CampaignId, cancellationToken);

        if (campaign == null)
        {
            return QueryResult<GetCampaignDetailQueryResult>.Failed();
        }

        if (request.SubdomainId != Guid.Empty && campaign.SubdomainId != request.SubdomainId)
        {
            return QueryResult<GetCampaignDetailQueryResult>.Failed();
        }

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
                    email.SentAt,
                    email.Subject);
            }
        }

        var timeBuckets = new List<GetCampaignDetailQueryResult.TimeBucket>
        {
            new(
                campaign.FirstReceivedAt,
                campaign.LastReceivedAt,
                campaign.TotalCaptured),
        };

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