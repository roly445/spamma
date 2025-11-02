using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.DisableChaosAddress;

[BluQubeCommand(Path = "api/domain-management/disable-chaos-address")]
public record DisableChaosAddressCommand(Guid Id, Guid ActorId) : ICommand;
