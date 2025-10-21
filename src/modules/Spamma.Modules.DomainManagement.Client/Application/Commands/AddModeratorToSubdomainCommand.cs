using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/subdomain-management/add-moderator")]
public record AddModeratorToSubdomainCommand(Guid SubdomainId, Guid UserId) : ICommand;