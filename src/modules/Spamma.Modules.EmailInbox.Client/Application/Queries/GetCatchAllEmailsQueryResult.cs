using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record GetCatchAllEmailsQueryResult(
    IReadOnlyList<GetCatchAllEmailsQueryResult.SenderGroup> Groups,
    int TotalCount) : IQueryResult
{
    public record SenderGroup(
        string SenderAddress,
        IReadOnlyList<EmailSummary> Emails);

    public record EmailSummary(
        Guid EmailId,
        string Subject,
        string PrimaryToAddress,
        DateTimeOffset ReceivedAt,
        bool IsFavorite);
}
