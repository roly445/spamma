using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record ExportCampaignDataQueryResult(string JobId, string? DownloadUrl) : IQueryResult;
