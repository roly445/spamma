using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record SearchChaosAddressesQueryResult(
    IReadOnlyList<ChaosAddressSummary> Items,
    int TotalCount,
    int PageNumber,
    int PageSize) : IQueryResult
{
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}
