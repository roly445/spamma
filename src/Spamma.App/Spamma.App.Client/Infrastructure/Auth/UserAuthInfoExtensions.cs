using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Infrastructure.Auth;

public static class UserAuthInfoExtensions
{
    public static bool IsDomainManager(this UserAuthInfo user)
    {
        return user.SystemRole.HasFlag(SystemRole.DomainManagement);
    }

    public static bool CanViewChaosAddresses(this UserAuthInfo user)
    {
        return user.IsAuthenticated;
    }

    public static bool CanCreateChaosAddresses(this UserAuthInfo user)
    {
        if (!user.IsAuthenticated)
        {
            return false;
        }

        if (user.IsDomainManager())
        {
            return true;
        }

        return user.ModeratedSubdomains.Count > 0 || user.ViewableSubdomains.Count > 0;
    }

    public static bool CanManageChaosAddresses(this UserAuthInfo user)
    {
        if (!user.IsAuthenticated)
        {
            return false;
        }

        if (user.IsDomainManager())
        {
            return true;
        }

        return user.ModeratedSubdomains.Count > 0;
    }
}

