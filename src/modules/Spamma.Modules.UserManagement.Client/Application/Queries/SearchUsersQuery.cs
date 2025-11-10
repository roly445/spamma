using BluQube.Attributes;
using BluQube.Queries;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/users/search")]
public record SearchUsersQuery(
    string? SearchTerm = null,
    UserStatus? Status = null,
    SystemRole? SystemRole = null,
    IReadOnlyDictionary<Guid, bool>? DomainFilters = null,
    IReadOnlyDictionary<Guid, bool>? SubdomainFilters = null,
    string SortBy = "CreatedAt",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 10) : IQuery<SearchUsersQueryResult>;