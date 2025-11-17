using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record GetEmailContentQueryResult(string Content, string ContentType) : IQueryResult;