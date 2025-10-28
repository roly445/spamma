using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

/// <summary>
/// Query to retrieve all passkeys for the authenticated user.
/// </summary>
[BluQubeQuery(Path = "api/users/passkeys/my")]
public record GetMyPasskeysQuery : IQuery<GetMyPasskeysQueryResult>;