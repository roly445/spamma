using BluQube.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

internal class GetEmailInboxSettingsQueryProcessor(IEmailInboxSettingsService settingsService)
    : IQueryProcessor<GetEmailInboxSettingsQuery, GetEmailInboxSettingsQueryResult>
{
    public async Task<QueryResult<GetEmailInboxSettingsQueryResult>> Handle(GetEmailInboxSettingsQuery request, CancellationToken cancellationToken)
    {
        var catchAllEnabled = await settingsService.GetCatchAllModeEnabledAsync(cancellationToken);
        return QueryResult<GetEmailInboxSettingsQueryResult>.Succeeded(
            new GetEmailInboxSettingsQueryResult(catchAllEnabled));
    }
}
