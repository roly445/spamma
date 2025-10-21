using BluQube.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record GetDetailedDomainByIdQueryResult(
    Guid Id,
    string DomainName,
    DomainStatus Status,
    bool IsVerified,
    DateTime CreatedAt,
    DateTime? VerifiedAt,
    int SubdomainCount,
    int AssignedUserCount,
    string? PrimaryContact,
    string? Description,
    bool IsSuspended,
    string VerificationToken,
    IReadOnlyList<GetDetailedDomainByIdQueryResult.BasicSubdomainListItem> Subdomains,
    IReadOnlyList<GetDetailedDomainByIdQueryResult.DomainUserAssignment> AssignedUsers) : IQueryResult
{
    public record BasicSubdomainListItem(
        Guid Id,
        string SubdomainName,
        string FullDomainName,
        bool IsActive,
        DateTime CreatedAt);

    public record DomainUserAssignment(
        Guid UserId,
        string Email,
        string? DisplayName,
        DateTime AssignedAt);
}