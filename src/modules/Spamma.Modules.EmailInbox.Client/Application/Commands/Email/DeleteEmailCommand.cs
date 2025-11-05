using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands;

[BluQubeCommand(Path = "email-inbox/delete-email")]
public record DeleteEmailCommand(Guid EmailId) : ICommand;