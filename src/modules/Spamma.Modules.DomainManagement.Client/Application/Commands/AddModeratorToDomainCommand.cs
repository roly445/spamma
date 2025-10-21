using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/domain-management/add-moderator")]
public record AddModeratorToDomainCommand(Guid DomainId, Guid UserId) : ICommand;