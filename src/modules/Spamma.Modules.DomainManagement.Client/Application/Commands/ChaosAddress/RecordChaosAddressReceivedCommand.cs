using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.RecordChaosAddressReceived;

[BluQubeCommand(Path = "api/domain-management/record-chaos-address-received")]
public record RecordChaosAddressReceivedCommand(Guid ChaosAddressId, DateTime ReceivedAt) : ICommand;
