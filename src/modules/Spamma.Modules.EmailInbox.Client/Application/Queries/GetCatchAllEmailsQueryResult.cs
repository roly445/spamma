using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record GetCatchAllEmailsQueryResult(
    IReadOnlyList<GetCatchAllEmailsQueryResult.DomainGroup> Groups,
    int TotalCount) : IQueryResult
{
    public record DomainGroup(
        string Domain,
        IReadOnlyList<EmailSummary> Emails);

    public record EmailSummary(
        Guid EmailId,
        string Subject,
        string PrimaryToAddress,
        DateTimeOffset ReceivedAt,
        bool IsFavorite);
}
