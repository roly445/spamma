using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

[BluQubeCommand(Path = "api/domain-management/enable-chaos-address")]
public record EnableChaosAddressCommand(Guid ChaosAddressId) : ICommand;