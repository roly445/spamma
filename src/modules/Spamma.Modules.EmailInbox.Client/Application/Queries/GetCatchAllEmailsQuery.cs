using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "api/email-inbox/catch-all")]
public record GetCatchAllEmailsQuery(
    int Page = 1,
    int PageSize = 50,
    string? SearchText = null) : IQuery<GetCatchAllEmailsQueryResult>;
