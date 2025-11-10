using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "email-inbox/search-emails")]
public record SearchEmailsQuery(
    string? SearchText = null,
    int Page = 1,
    int PageSize = 500,
    bool ShowCampaignEmails = false) : IQuery<SearchEmailsQueryResult>;