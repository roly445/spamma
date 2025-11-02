using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/domain-management/remove-moderator")]
public record RemoveModeratorFromDomainCommand(Guid DomainId, Guid UserId) : ICommand;