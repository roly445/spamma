using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/domain-management/update")]
public record UpdateDomainDetailsCommand(Guid DomainId, string? PrimaryContactEmail, string? Description) : ICommand;