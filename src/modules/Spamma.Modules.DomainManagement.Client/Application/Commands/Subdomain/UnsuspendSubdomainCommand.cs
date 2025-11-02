using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/subdomain-management/unsuspend")]
public record UnsuspendSubdomainCommand(Guid SubdomainId) : ICommand;