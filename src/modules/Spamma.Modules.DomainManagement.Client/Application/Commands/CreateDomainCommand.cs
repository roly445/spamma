using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/domain-management/create")]
public record CreateDomainCommand(Guid DomainId, string Name, string? PrimaryContactEmail, string? Description) : ICommand<CreateDomainCommandResult>;