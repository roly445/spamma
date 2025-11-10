using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

/// <summary>
/// Result for GetEmailContentQuery.
/// </summary>
public record GetEmailContentQueryResult(string Content, string ContentType) : IQueryResult;