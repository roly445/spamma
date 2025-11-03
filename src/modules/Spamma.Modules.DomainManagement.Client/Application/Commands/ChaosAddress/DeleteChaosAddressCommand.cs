using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.DeleteChaosAddress;

[BluQubeCommand(Path = "api/domain-management/delete-chaos-address")]
public record DeleteChaosAddressCommand(Guid Id) : ICommand;
