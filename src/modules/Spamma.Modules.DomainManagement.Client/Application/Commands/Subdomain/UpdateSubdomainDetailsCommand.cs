using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

[BluQubeCommand(Path = "api/subdomain-management/update")]
public record UpdateSubdomainDetailsCommand(Guid SubdomainId, string? Description) : ICommand;