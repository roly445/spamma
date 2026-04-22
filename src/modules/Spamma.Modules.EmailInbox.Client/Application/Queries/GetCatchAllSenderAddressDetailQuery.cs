using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

[BluQubeQuery(Path = "api/email-inbox/catch-all-senders/detail")]
public record GetCatchAllSenderAddressDetailQuery(Guid SenderAddressId) : IQuery<GetCatchAllSenderAddressDetailQueryResult>;
