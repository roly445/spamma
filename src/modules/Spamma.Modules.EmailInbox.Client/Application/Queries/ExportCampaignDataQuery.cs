using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "api/email-inbox/campaigns/export")]
public record ExportCampaignDataQuery(
    Guid SubdomainId,
    string CampaignValue,
    string Format = "csv",
    int? MaxRows = null) : IQuery<ExportCampaignDataQueryResult>;
