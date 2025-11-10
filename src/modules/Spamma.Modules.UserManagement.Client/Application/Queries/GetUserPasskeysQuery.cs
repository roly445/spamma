using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/users/passkeys")]
public record GetUserPasskeysQuery(Guid UserId) : IQuery<GetUserPasskeysQueryResult>;