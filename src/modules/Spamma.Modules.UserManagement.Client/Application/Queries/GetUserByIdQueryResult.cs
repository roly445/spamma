using BluQube.Queries;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

public record GetUserByIdQueryResult(
    Guid Id,
    string EmailAddress,
    string Name,
    bool IsSuspended,
    SystemRole SystemRole,
    IReadOnlyList<Guid> ModeratedDomains,
    IReadOnlyList<Guid> ModeratedSubdomains,
    IReadOnlyList<Guid> ViewableSubdomains) : IQueryResult;