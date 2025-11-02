using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

[BluQubeCommand(Path = "api/subdomain-management/unsuspend")]
public record UnsuspendSubdomainCommand(Guid SubdomainId) : ICommand;