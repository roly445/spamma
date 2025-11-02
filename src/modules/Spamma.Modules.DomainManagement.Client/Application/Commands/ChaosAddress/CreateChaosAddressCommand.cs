using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.CreateChaosAddress;

[BluQubeCommand(Path = "api/domain-management/create-chaos-address")]
public record CreateChaosAddressCommand(
    Guid Id,
    Guid DomainId,
    Guid SubdomainId,
    string LocalPart,
    Spamma.Modules.Common.SmtpResponseCode ConfiguredSmtpCode,
    Guid CreatedBy) : ICommand;
