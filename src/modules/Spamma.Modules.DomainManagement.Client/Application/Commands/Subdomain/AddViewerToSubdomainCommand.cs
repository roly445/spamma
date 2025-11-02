using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

[BluQubeCommand(Path = "api/subdomain-management/add-viewer")]
public record AddViewerToSubdomainCommand(Guid SubdomainId, Guid UserId) : ICommand;