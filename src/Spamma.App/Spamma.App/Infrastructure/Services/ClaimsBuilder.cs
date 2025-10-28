using System.Security.Claims;
using Spamma.App.Client.Infrastructure.Constants;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Infrastructure.Services;

/// <summary>
/// Service for building authentication claims from user data.
/// Consolidates claim construction logic into a single reusable method.
/// </summary>
internal static class ClaimsBuilder
{
    /// <summary>
    /// Builds a claims list from cached user data.
    /// </summary>
    /// <param name="user">The cached user data.</param>
    /// <param name="authMethod">The authentication method used (e.g., "magic_link", "passkey").</param>
    /// <returns>A list of claims for the authenticated user.</returns>
    internal static List<Claim> BuildClaims(UserStatusCache.CachedUser user, string authMethod = "magic_link")
    {
        var loginTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.EmailAddress),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.SystemRole.ToString()),
            new("auth_method", authMethod),
            new("login_time", loginTime),
        };

        foreach (var domain in user.ModeratedDomains)
        {
            claims.Add(new(Lookups.ModeratedDomainClaim, domain.ToString()));
        }

        foreach (var subdomain in user.ModeratedSubdomains)
        {
            claims.Add(new(Lookups.ModeratedSubdomainClaim, subdomain.ToString()));
        }

        foreach (var viewableSubdomain in user.ViewableSubdomains)
        {
            claims.Add(new(Lookups.ViewableSubdomainClaim, viewableSubdomain.ToString()));
        }

        return claims;
    }

    /// <summary>
    /// Builds a claims list from user query result data.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="emailAddress">The user's email address.</param>
    /// <param name="name">The user's display name.</param>
    /// <param name="systemRole">The user's system role.</param>
    /// <param name="moderatedDomains">Collection of domain IDs moderated by the user.</param>
    /// <param name="moderatedSubdomains">Collection of subdomain IDs moderated by the user.</param>
    /// <param name="viewableSubdomains">Collection of subdomain IDs viewable by the user.</param>
    /// <param name="authMethod">The authentication method used (e.g., "magic_link", "passkey").</param>
    /// <returns>A list of claims for the authenticated user.</returns>
    internal static List<Claim> BuildClaims(
        Guid userId,
        string emailAddress,
        string name,
        SystemRole systemRole,
        IEnumerable<Guid> moderatedDomains,
        IEnumerable<Guid> moderatedSubdomains,
        IEnumerable<Guid> viewableSubdomains,
        string authMethod = "magic_link")
    {
        var loginTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, emailAddress),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, systemRole.ToString()),
            new("auth_method", authMethod),
            new("login_time", loginTime),
        };

        foreach (var domain in moderatedDomains)
        {
            claims.Add(new(Lookups.ModeratedDomainClaim, domain.ToString()));
        }

        foreach (var subdomain in moderatedSubdomains)
        {
            claims.Add(new(Lookups.ModeratedSubdomainClaim, subdomain.ToString()));
        }

        foreach (var viewableSubdomain in viewableSubdomains)
        {
            claims.Add(new(Lookups.ViewableSubdomainClaim, viewableSubdomain.ToString()));
        }

        return claims;
    }
}