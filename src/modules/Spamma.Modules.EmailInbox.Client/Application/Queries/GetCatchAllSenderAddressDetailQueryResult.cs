using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record GetCatchAllSenderAddressDetailQueryResult(
    Guid Id,
    string SenderAddress,
    IReadOnlyList<Guid> AssignedUserIds,
    bool IsRemoved,
    DateTimeOffset AddedAt) : IQueryResult;
