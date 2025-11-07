using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class GetDetailedDomainByIdQueryProcessor(IDocumentSession session) : IQueryProcessor<GetDetailedDomainByIdQuery, GetDetailedDomainByIdQueryResult>
{
    public async Task<QueryResult<GetDetailedDomainByIdQueryResult>> Handle(GetDetailedDomainByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await session.Query<DomainLookup>()
            .FirstOrDefaultAsync(d => d.Id == request.DomainId, token: cancellationToken);

        if (result == null)
        {
            return QueryResult<GetDetailedDomainByIdQueryResult>.Failed();
        }

        var detailedResult = new GetDetailedDomainByIdQueryResult(
            result.Id,
            result.DomainName,
            GetDomainStatus(result),
            result.IsVerified,
            result.CreatedAt,
            result.VerifiedAt,
            result.SubdomainCount,
            result.AssignedModeratorCount,
            result.PrimaryContact,
            result.Description,
            result.IsSuspended,
            result.VerificationToken, new List<GetDetailedDomainByIdQueryResult.BasicSubdomainListItem>(), new List<GetDetailedDomainByIdQueryResult.DomainUserAssignment>());

        return QueryResult<GetDetailedDomainByIdQueryResult>.Succeeded(detailedResult);
    }

    private static DomainStatus GetDomainStatus(DomainLookup domain)
    {
        if (domain.IsSuspended)
        {
            return DomainStatus.Suspended;
        }

        return !domain.IsVerified ? DomainStatus.Pending : DomainStatus.Active;
    }
}