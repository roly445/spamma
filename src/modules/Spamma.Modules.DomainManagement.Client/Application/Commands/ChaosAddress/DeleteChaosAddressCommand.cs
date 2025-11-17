using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

[BluQubeCommand(Path = "api/domain-management/delete-chaos-address")]
public record DeleteChaosAddressCommand(Guid ChaosAddressId) : ICommand;