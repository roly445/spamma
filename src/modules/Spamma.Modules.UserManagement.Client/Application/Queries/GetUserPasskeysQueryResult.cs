using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

/// <summary>
/// Result containing specific user's passkeys.
/// </summary>
public record GetUserPasskeysQueryResult(IEnumerable<PasskeySummary> Passkeys) : IQueryResult;