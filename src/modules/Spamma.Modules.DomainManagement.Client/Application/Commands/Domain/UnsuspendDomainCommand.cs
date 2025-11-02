using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/domain-management/unsuspend")]
public record UnsuspendDomainCommand(Guid DomainId) : ICommand;