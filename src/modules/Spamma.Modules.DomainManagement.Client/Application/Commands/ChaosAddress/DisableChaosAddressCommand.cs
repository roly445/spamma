using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

[BluQubeCommand(Path = "api/domain-management/disable-chaos-address")]
public record DisableChaosAddressCommand(Guid ChaosAddressId) : ICommand;