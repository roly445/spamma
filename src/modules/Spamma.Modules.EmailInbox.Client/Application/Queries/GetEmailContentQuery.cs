using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

/// <summary>
/// Query to get full email content.
/// </summary>
[BluQubeQuery(Path = "api/email-push/emails/{EmailId}")]
public record GetEmailContentQuery(Guid EmailId) : IQuery<GetEmailContentQueryResult>;