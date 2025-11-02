using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/subdomain-management/remove-viewer")]
public record RemoveViewerFromSubdomainCommand(Guid SubdomainId, Guid UserId) : ICommand;