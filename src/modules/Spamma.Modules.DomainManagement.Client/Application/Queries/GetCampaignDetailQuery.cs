using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/domains/campaigns/{campaignId}")]
public record GetCampaignDetailQuery(Guid SubdomainId, Guid CampaignId, int? DaysBucket = 7) : IQuery<GetCampaignDetailQueryResult>;
