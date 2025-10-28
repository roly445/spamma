using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

/// <summary>
/// Query to retrieve all passkeys for a specific user (admin only).
/// </summary>
[BluQubeQuery(Path = "api/users/passkeys")]
public record GetUserPasskeysQuery(Guid UserId) : IQuery<GetUserPasskeysQueryResult>;