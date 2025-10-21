using BluQube.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record GetDetailedSubdomainByIdQueryResult(
    Guid SubdomainId,
    string Name,
    string? Description,
    SubdomainStatus Status,
    DateTime CreatedAt,
    string DomainName,
    string FullName,
    Guid DomainId,
    MxStatus MxStatus,
    DateTime? MxCheckedAt) : IQueryResult;