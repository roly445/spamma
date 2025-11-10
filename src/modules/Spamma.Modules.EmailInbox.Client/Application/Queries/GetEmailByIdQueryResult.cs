using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record GetEmailByIdQueryResult(
    Guid Id,
    Guid SubdomainId,
    string Subject,
    DateTimeOffset WhenSent,
    bool IsFavorite,
    Guid? CampaignId) : IQueryResult;