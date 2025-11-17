using BluQube.Queries;

namespace Spamma.Modules.DomainManagement.Client.Application.Queries;

public record SearchChaosAddressesQueryResult(
    IReadOnlyList<ChaosAddressSummary> Items,
    int TotalCount,
    int PageNumber,
    int PageSize) : IQueryResult
{
    public int TotalPages => (this.TotalCount + this.PageSize - 1) / this.PageSize;
}