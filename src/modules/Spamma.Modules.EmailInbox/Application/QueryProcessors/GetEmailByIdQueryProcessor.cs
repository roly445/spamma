using BluQube.Queries;
using Marten;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

public class GetEmailByIdQueryProcessor(IDocumentSession session) : IQueryProcessor<GetEmailByIdQuery, GetEmailByIdQueryResult>
{
    public async Task<QueryResult<GetEmailByIdQueryResult>> Handle(GetEmailByIdQuery request, CancellationToken cancellationToken)
    {
        var email = await session.Query<EmailLookup>()
            .FirstOrDefaultAsync(e => e.Id == request.EmailId, cancellationToken);

        if (email == null)
        {
            return QueryResult<GetEmailByIdQueryResult>.Failed();
        }

        var result = new GetEmailByIdQueryResult(
            email.Id,
            email.SubdomainId,
            email.Subject,
            email.WhenSent,
            email.IsFavorite,
            email.CampaignId);

        return QueryResult<GetEmailByIdQueryResult>.Succeeded(result);
    }
}
