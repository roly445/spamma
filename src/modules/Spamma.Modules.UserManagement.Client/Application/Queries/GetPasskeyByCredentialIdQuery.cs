using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/users/passkeys/by-credential-id")]
public record GetPasskeyByCredentialIdQuery(byte[] CredentialId) : IQuery<GetPasskeyByCredentialIdQueryResult>;