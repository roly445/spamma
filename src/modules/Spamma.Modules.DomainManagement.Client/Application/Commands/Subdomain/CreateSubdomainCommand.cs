using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

[BluQubeCommand(Path = "api/subdomain-management/create")]
public record CreateSubdomainCommand(Guid SubdomainId, Guid DomainId, string Name, string? Description) : ICommand;