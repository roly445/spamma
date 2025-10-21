using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

public record GetUserByIdQuery(Guid UserId) : IQuery<GetUserByIdQueryResult>;