using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

/// <summary>
/// Requirement to check if the user has viewer access to a subdomain.
/// </summary>
public class MustHaveViewerAccessToSubdomainRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the subdomain ID.
    /// </summary>
    public required Guid SubdomainId { get; init; }
}