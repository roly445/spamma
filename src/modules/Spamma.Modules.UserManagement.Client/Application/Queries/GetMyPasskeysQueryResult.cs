using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

public record GetMyPasskeysQueryResult(IEnumerable<PasskeySummary> Passkeys) : IQueryResult;