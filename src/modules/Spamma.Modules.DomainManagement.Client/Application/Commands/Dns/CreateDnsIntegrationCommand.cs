using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Dns;

[BluQubeCommand(Path = "api/domain-management/dns-integrations/create")]
public record CreateDnsIntegrationCommand(Guid IntegrationId, string Name, string Provider, string ZoneName, string ZoneId, string Credential) : ICommand;
