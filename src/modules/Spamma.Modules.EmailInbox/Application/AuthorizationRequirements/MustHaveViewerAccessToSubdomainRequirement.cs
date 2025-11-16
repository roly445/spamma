using MediatR.Behaviors.Authorization;

namespace Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

public class MustHaveViewerAccessToSubdomainRequirement : IAuthorizationRequirement
{
    public required Guid SubdomainId { get; init; }
}