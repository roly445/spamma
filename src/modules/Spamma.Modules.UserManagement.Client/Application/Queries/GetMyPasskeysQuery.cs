using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/users/passkeys/my")]
public record GetMyPasskeysQuery : IQuery<GetMyPasskeysQueryResult>;