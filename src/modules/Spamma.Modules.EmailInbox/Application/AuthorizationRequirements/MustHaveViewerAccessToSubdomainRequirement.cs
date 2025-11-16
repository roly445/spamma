using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

public class MustHaveViewerAccessToSubdomainRequirement : IAuthorizationRequirement
{
    public required Guid SubdomainId { get; init; }
}