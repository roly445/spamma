using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

public record RecordChaosAddressReceivedCommand(Guid ChaosAddressId, DateTimeOffset ReceivedAt) : ICommand;
