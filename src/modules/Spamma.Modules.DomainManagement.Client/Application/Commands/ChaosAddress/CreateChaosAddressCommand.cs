using BluQube.Attributes;
using BluQube.Commands;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

[BluQubeCommand(Path = "api/domain-management/create-chaos-address")]
public record CreateChaosAddressCommand(
    Guid ChaosAddressId,
    Guid DomainId,
    Guid SubdomainId,
    string LocalPart,
    SmtpResponseCode ConfiguredSmtpCode) : ICommand;