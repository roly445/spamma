using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record ExportJobStatusQueryResult(string JobId, string Status, int PercentComplete, string? DownloadUrl) : IQueryResult;
