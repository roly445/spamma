using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.EnableChaosAddress;

[BluQubeCommand(Path = "api/domain-management/enable-chaos-address")]
public record EnableChaosAddressCommand(Guid Id, Guid ActorId) : ICommand;
