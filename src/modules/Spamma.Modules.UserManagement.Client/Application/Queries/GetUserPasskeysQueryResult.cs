using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

public record GetUserPasskeysQueryResult(IEnumerable<PasskeySummary> Passkeys) : IQueryResult;