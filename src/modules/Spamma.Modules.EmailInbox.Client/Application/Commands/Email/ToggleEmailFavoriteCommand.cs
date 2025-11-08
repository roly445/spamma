using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands;

[BluQubeCommand(Path = "email-inbox/toggle-favorite")]
public record ToggleEmailFavoriteCommand(Guid EmailId) : ICommand;