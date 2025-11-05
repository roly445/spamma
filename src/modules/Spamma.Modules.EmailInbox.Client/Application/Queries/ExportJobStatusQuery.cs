using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "api/email-inbox/campaigns/export/status/{JobId}")]
public record ExportJobStatusQuery(string JobId) : IQuery<ExportJobStatusQueryResult>;
