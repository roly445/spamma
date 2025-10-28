using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

/// <summary>
/// Query to get details of a specific passkey.
/// </summary>
[BluQubeQuery(Path = "api/users/passkeys/details")]
public record GetPasskeyDetailsQuery(Guid PasskeyId) : IQuery<PasskeyDetailsResult>;