using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

internal class ListDnsIntegrationsQueryProcessor(IDocumentSession session) : IQueryProcessor<ListDnsIntegrationsQuery, ListDnsIntegrationsQueryResult>
{
    public async ValueTask<QueryResult<ListDnsIntegrationsQueryResult>> Handle(ListDnsIntegrationsQuery request, CancellationToken cancellationToken)
    {
        var integrations = await session.Query<DnsIntegration>().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var summaries = integrations.Select(x => new DnsIntegrationSummary(x.Id, x.Name, x.Provider, x.ZoneName, x.ZoneId)).ToList();
        return QueryResult<ListDnsIntegrationsQueryResult>.Succeeded(new(summaries));
    }
}
