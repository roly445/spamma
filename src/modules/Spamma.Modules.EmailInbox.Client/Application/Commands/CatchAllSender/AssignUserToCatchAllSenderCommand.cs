using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

[BluQubeCommand(Path = "api/email-inbox/catch-all-senders/assign-user")]
public record AssignUserToCatchAllSenderCommand(Guid SenderAddressId, Guid UserId) : ICommand;
