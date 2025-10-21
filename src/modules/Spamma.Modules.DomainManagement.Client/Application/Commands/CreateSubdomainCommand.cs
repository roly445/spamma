using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/subdomain-management/create")]
public record CreateSubdomainCommand(Guid SubdomainId, Guid DomainId, string Name, string? Description) : ICommand;