using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record SearchCatchAllSenderAddressesQueryResult(
    IReadOnlyList<SearchCatchAllSenderAddressesQueryResult.SenderAddressSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages) : IQueryResult
{
    public record SenderAddressSummary(
        Guid Id,
        string SenderAddress,
        int AssignedUserCount,
        bool IsRemoved,
        DateTimeOffset AddedAt);
}
