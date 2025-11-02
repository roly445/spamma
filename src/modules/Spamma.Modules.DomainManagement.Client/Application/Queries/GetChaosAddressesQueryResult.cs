using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record GetChaosAddressesQueryResult(
    IReadOnlyList<ChaosAddressSummary> Items,
    int TotalCount,
    int PageNumber,
    int PageSize) : IQueryResult;
