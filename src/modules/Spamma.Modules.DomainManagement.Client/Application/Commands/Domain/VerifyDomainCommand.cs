using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

[BluQubeCommand(Path = "api/domain-management/verify")]
public record VerifyDomainCommand(Guid DomainId) : ICommand;