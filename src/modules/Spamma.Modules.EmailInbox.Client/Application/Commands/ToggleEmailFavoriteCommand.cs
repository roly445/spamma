using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands;

/// <summary>
/// Command to toggle the favorite status of an email to prevent/allow automatic deletion.
/// </summary>
[BluQubeCommand(Path = "email-inbox/toggle-favorite")]
public record ToggleEmailFavoriteCommand(Guid EmailId) : ICommand;