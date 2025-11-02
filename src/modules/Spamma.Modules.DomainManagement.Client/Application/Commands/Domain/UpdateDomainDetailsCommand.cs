using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

[BluQubeCommand(Path = "api/domain-management/update")]
public record UpdateDomainDetailsCommand(Guid DomainId, string? PrimaryContactEmail, string? Description) : ICommand;