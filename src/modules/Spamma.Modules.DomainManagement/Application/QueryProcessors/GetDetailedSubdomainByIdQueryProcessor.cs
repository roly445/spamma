using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

internal class GetDetailedSubdomainByIdQueryProcessor(IDocumentSession session, IHttpContextAccessor accessor) : IQueryProcessor<GetDetailedSubdomainByIdQuery, GetDetailedSubdomainByIdQueryResult>
{
    public async ValueTask<QueryResult<GetDetailedSubdomainByIdQueryResult>> Handle(GetDetailedSubdomainByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await session.Query<SubdomainLookup>()
            .FirstOrDefaultAsync(d => d.Id == request.SubdomainId, token: cancellationToken);

        if (result == null)
        {
            return QueryResult<GetDetailedSubdomainByIdQueryResult>.Failed();
        }

        var parent = await session.LoadAsync<DomainLookup>(result.DomainId, cancellationToken);
        var user = accessor.HttpContext.ToUserAuthInfo();
        var canSetupDns = parent?.DnsIntegrationId.HasValue == true &&
            (user.SystemRole.HasFlag(SystemRole.DomainManagement) || user.ModeratedDomains.Contains(result.DomainId) || user.ModeratedSubdomains.Contains(result.Id));

        var detailedResult = new GetDetailedSubdomainByIdQueryResult(
            result.Id,
            result.SubdomainName,
            result.Description,
            GetStatus(result),
            result.CreatedAt,
            result.ParentName,
            result.FullName,
            result.DomainId,
            result.MxStatus,
            result.MxLastCheckedAt,
            canSetupDns);

        return QueryResult<GetDetailedSubdomainByIdQueryResult>.Succeeded(detailedResult);
    }

    private static SubdomainStatus GetStatus(SubdomainLookup domain)
    {
        return domain.IsSuspended ? SubdomainStatus.Suspended : SubdomainStatus.Active;
    }
}
