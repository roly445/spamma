using BluQube.Queries;
using Spamma.Modules.Common.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

internal class GetSystemSettingsQueryProcessor(IEmailInboxSettingsService settingsService)
    : IQueryProcessor<GetSystemSettingsQuery, GetSystemSettingsQueryResult>
{
    public async ValueTask<QueryResult<GetSystemSettingsQueryResult>> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        var catchAllEnabled = await settingsService.GetCatchAllModeEnabledAsync(cancellationToken);
        return QueryResult<GetSystemSettingsQueryResult>.Succeeded(
            new GetSystemSettingsQueryResult(catchAllEnabled));
    }
}
