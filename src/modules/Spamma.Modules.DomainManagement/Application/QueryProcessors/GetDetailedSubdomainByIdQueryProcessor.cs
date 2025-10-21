using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class GetDetailedSubdomainByIdQueryProcessor(IDocumentSession session) : IQueryProcessor<GetDetailedSubdomainByIdQuery, GetDetailedSubdomainByIdQueryResult>
{
    public async Task<QueryResult<GetDetailedSubdomainByIdQueryResult>> Handle(GetDetailedSubdomainByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await session.Query<SubdomainLookup>()
            .FirstOrDefaultAsync(d => d.Id == request.SubdomainId, token: cancellationToken);

        if (result == null)
        {
            return QueryResult<GetDetailedSubdomainByIdQueryResult>.Failed();
        }

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
            result.MxLastCheckedAt);

        return QueryResult<GetDetailedSubdomainByIdQueryResult>.Succeeded(detailedResult);
    }

    private static SubdomainStatus GetStatus(SubdomainLookup domain)
    {
        if (domain.IsSuspended)
        {
            return SubdomainStatus.Suspended;
        }

        return SubdomainStatus.Active;
    }
}