using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.EditChaosAddress;

[BluQubeCommand(Path = "api/domain-management/edit-chaos-address")]
public record EditChaosAddressCommand(
    Guid Id,
    Guid DomainId,
    Guid SubdomainId,
    string LocalPart,
    Spamma.Modules.Common.SmtpResponseCode ConfiguredSmtpCode,
    string? Description) : ICommand;
