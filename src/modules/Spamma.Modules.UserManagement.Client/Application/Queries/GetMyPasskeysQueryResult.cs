using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

/// <summary>
/// Result containing authenticated user's passkeys.
/// </summary>
public record GetMyPasskeysQueryResult(IEnumerable<PasskeySummary> Passkeys) : IQueryResult;