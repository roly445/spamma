using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "api/email-inbox/settings")]
public record GetEmailInboxSettingsQuery : IQuery<GetEmailInboxSettingsQueryResult>;
