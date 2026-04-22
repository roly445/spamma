using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

[BluQubeCommand(Path = "api/email-inbox/catch-all-senders/unassign-user")]
public record UnassignUserFromCatchAllSenderCommand(Guid SenderAddressId, Guid UserId) : ICommand;
