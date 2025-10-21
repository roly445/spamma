using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/subdomain-management/update")]
public record UpdateSubdomainDetailsCommand(Guid SubdomainId, string? Description) : ICommand;