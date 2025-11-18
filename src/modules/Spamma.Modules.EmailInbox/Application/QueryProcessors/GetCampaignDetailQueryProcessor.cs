using BluQube.Queries;
using Marten;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
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
            // Load minimal scalar fields for the sample message
            var email = await session.Query<EmailLookup>()
                .Where(e => e.Id == sampleMessageId.Value)
                .Select(e => new { e.Id, e.Subject, e.SentAt })
                .FirstOrDefaultAsync(cancellationToken);

            if (email != null)
            {
                // Derive From/To with separate subqueries against the child collection to avoid materialization issues
                var from = await session.Query<EmailLookup>()
                    .Where(e => e.Id == sampleMessageId.Value)
                    .SelectMany(e => e.EmailAddresses)
                    .Where(a => a.EmailAddressType == EmailAddressType.From)
                    .Select(a => a.Address)
                    .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

                var to = await session.Query<EmailLookup>()
                    .Where(e => e.Id == sampleMessageId.Value)
                    .SelectMany(e => e.EmailAddresses)
                    .Where(a => a.EmailAddressType == EmailAddressType.To)
                    .Select(a => a.Address)
                    .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

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