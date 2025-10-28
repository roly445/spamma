using System.Linq.Expressions;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Application.QueryProcessors.User;

public class SearchUsersQueryProcessor(IDocumentSession session, IHttpContextAccessor accessor, IOptions<Settings> settings)
    : IQueryProcessor<SearchUsersQuery, SearchUsersQueryResult>
{
    public async Task<QueryResult<SearchUsersQueryResult>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        Guid? userId = null;
        var rawUserId = accessor.HttpContext?.User.FindFirst("user_id")?.Value;
        if (rawUserId != null && Guid.TryParse(rawUserId, out var outUserId))
        {
            userId = outUserId;
        }

        var baseQuery = session.Query<UserLookup>();

        var whereConditions = new List<Expression<Func<UserLookup, bool>>>();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            whereConditions.Add(u => u.Name.Contains(request.SearchTerm) || u.EmailAddress.Contains(request.SearchTerm));
        }

        if (request.SystemRole.HasValue)
        {
            whereConditions.Add(u => u.SystemRole == request.SystemRole);
        }

        if (request.Status.HasValue)
        {
            switch (request.Status.Value)
            {
                case UserStatus.Active:
                    whereConditions.Add(u => !u.IsSuspended);
                    break;
                case UserStatus.Suspended:
                    whereConditions.Add(u => u.IsSuspended);
                    break;
                case UserStatus.Inactive:
                    whereConditions.Add(u => u.LastLoginAt == null || u.LastLoginAt < DateTime.UtcNow.AddMonths(-1));
                    break;
            }
        }

        IQueryable<UserLookup> filteredQuery = baseQuery;
        foreach (var condition in whereConditions)
        {
            filteredQuery = filteredQuery.Where(condition);
        }

        var orderedQuery = request.SortBy switch
        {
            "Email" => request.SortDescending
                ? filteredQuery.OrderByDescending(u => u.EmailAddress)
                : filteredQuery.OrderBy(u => u.EmailAddress),
            "DisplayName" => request.SortDescending
                ? filteredQuery.OrderByDescending(u => u.Name)
                : filteredQuery.OrderBy(u => u.Name),
            "LastLoginAt" => request.SortDescending
                ? filteredQuery.OrderByDescending(u => u.LastLoginAt)
                : filteredQuery.OrderBy(u => u.LastLoginAt),
            "Status" => request.SortDescending
                ? filteredQuery.OrderByDescending(u => u.IsSuspended)
                : filteredQuery.OrderBy(u => u.IsSuspended),
            _ => request.SortDescending
                ? filteredQuery.OrderByDescending(u => u.CreatedAt)
                : filteredQuery.OrderBy(u => u.CreatedAt),
        };

        // Get total count and paginated results
        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map to result DTOs
        var userSummaries = items.Select(u => new SearchUsersQueryResult.UserSummary(
            u.Id,
            u.EmailAddress,
            u.Name,
            u.CreatedAt,
            u.LastLoginAt,
            GetUserStatus(u),
            u.IsSuspended,
            u.WhenSuspended,
            u.Id == settings.Value.PrimaryUserId || (userId.HasValue && u.Id == userId.Value),
            u.SystemRole)).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return QueryResult<SearchUsersQueryResult>.Succeeded(new SearchUsersQueryResult(
            userSummaries,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages));
    }

    private static UserStatus GetUserStatus(UserLookup user)
    {
        if (user.IsSuspended)
        {
            return UserStatus.Suspended;
        }

        return user.LastLoginAt == null ? UserStatus.Inactive : UserStatus.Active;
    }
}