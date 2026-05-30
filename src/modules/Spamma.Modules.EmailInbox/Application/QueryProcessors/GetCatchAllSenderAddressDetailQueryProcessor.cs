using BluQube.Queries;
using Marten;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

internal class GetCatchAllSenderAddressDetailQueryProcessor(IDocumentSession documentSession)
    : IQueryProcessor<GetCatchAllSenderAddressDetailQuery, GetCatchAllSenderAddressDetailQueryResult>
{
    public async ValueTask<QueryResult<GetCatchAllSenderAddressDetailQueryResult>> Handle(GetCatchAllSenderAddressDetailQuery request, CancellationToken cancellationToken)
    {
        var lookup = await documentSession.Query<CatchAllSenderAddressLookup>()
            .FirstOrDefaultAsync(x => x.Id == request.SenderAddressId, cancellationToken);

        if (lookup == null)
        {
            return QueryResult<GetCatchAllSenderAddressDetailQueryResult>.Failed();
        }

        return QueryResult<GetCatchAllSenderAddressDetailQueryResult>.Succeeded(
            new GetCatchAllSenderAddressDetailQueryResult(
                lookup.Id,
                lookup.SenderAddress,
                lookup.AssignedUserIds,
                lookup.IsRemoved,
                lookup.AddedAt));
    }
}

