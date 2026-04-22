using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "api/email-inbox/catch-all-senders")]
public record SearchCatchAllSenderAddressesQuery(
    int Page = 1,
    int PageSize = 50) : IQuery<SearchCatchAllSenderAddressesQueryResult>;
