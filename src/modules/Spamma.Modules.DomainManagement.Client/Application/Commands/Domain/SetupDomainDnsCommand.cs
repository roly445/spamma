using BluQube.Attributes;
using BluQube.Commands;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

[BluQubeCommand(Path = "api/domain-management/setup-dns")]
public record SetupDomainDnsCommand(Guid DomainId) : ICommand<SetupDomainDnsCommandResult>;
