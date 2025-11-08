using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/users/passkeys/details")]
public record GetPasskeyDetailsQuery(Guid PasskeyId) : IQuery<PasskeyDetailsResult>;