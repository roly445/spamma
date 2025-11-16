using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "api/email-inbox/campaigns/{campaignId}")]
public record GetCampaignDetailQuery(
    Guid SubdomainId, Guid CampaignId, int? DaysBucket = 7) : IQuery<GetCampaignDetailQueryResult>;