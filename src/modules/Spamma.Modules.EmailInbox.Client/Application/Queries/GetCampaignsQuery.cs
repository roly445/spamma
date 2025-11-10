using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "api/email-inbox/campaigns")]
public record GetCampaignsQuery(
    Guid SubdomainId, int Page = 1, int PageSize = 50, string SortBy = "LastReceivedAt", bool SortDescending = true) :
    IQuery<GetCampaignsQueryResult>;
