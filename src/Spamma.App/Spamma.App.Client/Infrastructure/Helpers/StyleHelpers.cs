using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Infrastructure.Helpers;

internal static class StyleHelpers
{
    internal static string GetStatusClasses(SubdomainStatus status) => status switch
    {
        SubdomainStatus.Active => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-green-100 text-green-800",
        SubdomainStatus.Inactive => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
        SubdomainStatus.Suspended => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-red-100 text-red-800",
        _ => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
    };

    internal static string GetStatusClasses(DomainStatus status) => status switch
    {
        DomainStatus.Active => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-green-100 text-green-800",
        DomainStatus.Pending => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
        DomainStatus.Suspended => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-red-100 text-red-800",
        _ => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
    };

    internal static string GetStatusClasses(UserStatus status) => status switch
    {
        UserStatus.Active => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-green-100 text-green-800",
        UserStatus.Inactive => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
        UserStatus.Suspended => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-red-100 text-red-800",
        _ => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
    };

    internal static string GetStatusText(SubdomainStatus status) => status switch
    {
        SubdomainStatus.Active => "Active",
        SubdomainStatus.Inactive => "Inactive",
        SubdomainStatus.Suspended => "Suspended",
        _ => "Unknown",
    };

    internal static string GetStatusText(DomainStatus status) => status switch
    {
        DomainStatus.Active => "Active",
        DomainStatus.Pending => "Pending",
        DomainStatus.Suspended => "Suspended",
        _ => "Unknown",
    };

    internal static string GetStatusText(UserStatus status) => status switch
    {
        UserStatus.Active => "Active",
        UserStatus.Inactive => "Inactive",
        UserStatus.Suspended => "Suspended",
        _ => "Unknown",
    };
}