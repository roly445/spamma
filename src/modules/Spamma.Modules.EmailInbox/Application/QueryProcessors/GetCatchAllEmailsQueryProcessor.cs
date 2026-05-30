using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

internal class GetCatchAllEmailsQueryProcessor(IDocumentSession documentSession, IHttpContextAccessor accessor)
    : IQueryProcessor<GetCatchAllEmailsQuery, GetCatchAllEmailsQueryResult>
{
    public async ValueTask<QueryResult<GetCatchAllEmailsQueryResult>> Handle(GetCatchAllEmailsQuery request, CancellationToken cancellationToken)
    {
        var catchAllSubdomainId = EmailInboxSettingsDocument.CatchAllSubdomainId;
        var user = accessor.HttpContext.ToUserAuthInfo();
        var isDomainAdmin = user.SystemRole.HasFlag(SystemRole.DomainManagement);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize));
        var skip = (page - 1) * pageSize;

        var baseQuery = documentSession.Query<EmailLookup>()
            .Where(x => x.SubdomainId == catchAllSubdomainId && x.DeletedAt == null);

        if (!isDomainAdmin)
        {
            var assignedSenderAddresses = await documentSession.Query<CatchAllSenderAddressLookup>()
                .Where(x => x.AssignedUserIds.Contains(user.UserId) && !x.IsRemoved)
                .Select(x => x.Id)
                .ToListAsync(token: cancellationToken);

            var allowedIds = new HashSet<Guid>(assignedSenderAddresses);

            baseQuery = baseQuery.Where(x =>
                x.CatchAllSenderAddressId != null && allowedIds.Contains(x.CatchAllSenderAddressId.Value));
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var emails = await baseQuery
            .OrderByDescending(x => x.SentAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(token: cancellationToken);

        var groups = emails
            .GroupBy(e => e.EmailAddresses.FirstOrDefault(a => a.EmailAddressType == EmailAddressType.From)?.Address ?? string.Empty)
            .Select(g => new GetCatchAllEmailsQueryResult.SenderGroup(
                g.Key,
                g.Select(e =>
                {
                    var toAddress = e.EmailAddresses.FirstOrDefault(a => a.EmailAddressType == EmailAddressType.To)?.Address ?? string.Empty;
                    return new GetCatchAllEmailsQueryResult.EmailSummary(e.Id, e.Subject, toAddress, e.SentAt, e.IsFavorite);
                }).ToList()))
            .ToList();

        return QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
            new GetCatchAllEmailsQueryResult(groups, totalCount));
    }
}

