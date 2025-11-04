using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors.ExportJobStatus;

internal class ExportJobStatusQueryProcessor : QueryProcessor<Spamma.Modules.EmailInbox.Client.Application.Queries.ExportJobStatusQuery, Spamma.Modules.EmailInbox.Client.Application.Queries.ExportJobStatusQueryResult>
{
    public ExportJobStatusQueryProcessor()
    {
    }

    public override Task<QueryResult<Spamma.Modules.EmailInbox.Client.Application.Queries.ExportJobStatusQueryResult>> Handle(Spamma.Modules.EmailInbox.Client.Application.Queries.ExportJobStatusQuery query, CancellationToken cancellationToken)
    {
        // TODO: Query job store for status; return placeholder
        var result = new Spamma.Modules.EmailInbox.Client.Application.Queries.ExportJobStatusQueryResult(query.JobId, "Pending", 0, null);
        return Task.FromResult(QueryResult<Spamma.Modules.EmailInbox.Client.Application.Queries.ExportJobStatusQueryResult>.Succeeded(result));
    }
}
