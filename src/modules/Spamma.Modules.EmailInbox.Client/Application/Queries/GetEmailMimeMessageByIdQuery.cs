using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "email-inbox/get-email-mime-message-by-id")]
public record GetEmailMimeMessageByIdQuery(Guid EmailId) : IQuery<GetEmailMimeMessageByIdQueryResult>;