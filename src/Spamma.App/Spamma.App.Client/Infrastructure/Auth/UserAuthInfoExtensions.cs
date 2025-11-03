using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Infrastructure.Auth;

/// <summary>
/// Extension methods for UserAuthInfo to check authorization permissions.
/// </summary>
public static class UserAuthInfoExtensions
{
    /// <summary>
    /// Checks if the user has DomainManagement system role.
    /// </summary>
    public static bool IsDomainManager(this UserAuthInfo user)
    {
        return user.SystemRole.HasFlag(SystemRole.DomainManagement);
    }

    /// <summary>
    /// Checks if the user can view chaos addresses (any authenticated user).
    /// </summary>
    public static bool CanViewChaosAddresses(this UserAuthInfo user)
    {
        return user.IsAuthenticated;
    }

    /// <summary>
    /// Checks if the user can create chaos addresses.
    /// Domain managers can always create. Others need at least one moderated/viewable subdomain.
    /// </summary>
    public static bool CanCreateChaosAddresses(this UserAuthInfo user)
    {
        if (!user.IsAuthenticated)
        {
            return false;
        }

        // Domain managers can always create
        if (user.IsDomainManager())
        {
            return true;
        }

        // Otherwise, must have at least one subdomain (moderated or viewable)
        return user.ModeratedSubdomains.Count > 0 || user.ViewableSubdomains.Count > 0;
    }

    /// <summary>
    /// Checks if the user can manage (enable/disable/delete) chaos addresses.
    /// Domain managers can always manage. Others need at least one moderated subdomain.
    /// </summary>
    public static bool CanManageChaosAddresses(this UserAuthInfo user)
    {
        if (!user.IsAuthenticated)
        {
            return false;
        }

        // Domain managers can always manage
        if (user.IsDomainManager())
        {
            return true;
        }

        // Otherwise, must have at least one moderated subdomain (not just viewable)
        return user.ModeratedSubdomains.Count > 0;
    }
}
