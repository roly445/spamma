using BluQube.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

public class ExportJobStatusQueryProcessor : IQueryProcessor<ExportJobStatusQuery, ExportJobStatusQueryResult>
{
    public async Task<QueryResult<ExportJobStatusQueryResult>> Handle(ExportJobStatusQuery request, CancellationToken cancellationToken)
    {
        // Placeholder: query job store for export status
        var result = new ExportJobStatusQueryResult(request.JobId, "Pending", 0, null);
        return await Task.FromResult(QueryResult<ExportJobStatusQueryResult>.Succeeded(result));
    }
}
