using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries.ApiKeys;

[BluQubeQuery(Path = "api/user-management/api-keys/my")]
public record GetMyApiKeysQuery : IQuery<GetMyApiKeysQueryResult>;