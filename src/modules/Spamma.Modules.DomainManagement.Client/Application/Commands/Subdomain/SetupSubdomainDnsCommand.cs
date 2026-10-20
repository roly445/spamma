using BluQube.Attributes;
using BluQube.Commands;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

[BluQubeCommand(Path = "api/subdomain-management/setup-dns")]
public record SetupSubdomainDnsCommand(Guid SubdomainId) : ICommand<SetupSubdomainDnsCommandResult>;
