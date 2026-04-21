using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

[BluQubeCommand(Path = "api/email-inbox/settings/catch-all")]
public record UpdateCatchAllModeCommand(bool CatchAllModeEnabled) : ICommand;
