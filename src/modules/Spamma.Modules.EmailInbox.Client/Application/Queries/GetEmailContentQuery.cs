using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "api/email-push/emails/{EmailId}")]
public record GetEmailContentQuery(Guid EmailId) : IQuery<GetEmailContentQueryResult>;