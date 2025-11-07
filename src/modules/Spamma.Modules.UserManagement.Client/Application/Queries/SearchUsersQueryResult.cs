using BluQube.Queries;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

public record SearchUsersQueryResult(
    IReadOnlyList<SearchUsersQueryResult.UserSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages) : IQueryResult
{
    public bool HasNextPage => this.Page < this.TotalPages;

    public bool HasPreviousPage => this.Page > 1;

    public record UserSummary(
        Guid Id,
        string Email,
        string? DisplayName,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        UserStatus Status,
        bool IsSuspended,
        DateTime? WhenSuspended,
        bool IsReadOnly,
        SystemRole SystemRole);
}