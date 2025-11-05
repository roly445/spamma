using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "email-inbox/emails/{emailId}")]
public record GetEmailByIdQuery(Guid EmailId) : IQuery<GetEmailByIdQueryResult>;

public record GetEmailByIdQueryResult(
    Guid Id,
    Guid SubdomainId,
    string Subject,
    DateTime WhenSent,
    bool IsFavorite) : IQueryResult;
