using System.Net;

namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public static class WebhookDestinationPolicy
{
    public static bool IsAllowedUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            uri.Port != 443 ||
            uri.HostNameType != UriHostNameType.Dns ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        var host = uri.DnsSafeHost;
        return host.Contains('.', StringComparison.Ordinal) &&
               !host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
               !host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) &&
               !host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsUnderParentDomain(string value, string parentDomain)
    {
        var host = new Uri(value).DnsSafeHost;
        return host.Equals(parentDomain, StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith($".{parentDomain}", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return !IPAddress.IsLoopback(address) &&
                   !address.IsIPv6LinkLocal &&
                   !address.IsIPv6SiteLocal &&
                   !address.IsIPv6Multicast &&
                   (bytes[0] & 0xe0) == 0x20;
        }

        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return false;
        }

        var octets = address.GetAddressBytes();
        return octets[0] is >= 1 and <= 223 &&
               octets[0] != 10 &&
               octets[0] != 127 &&
               octets[0] != 169 &&
               !(octets[0] == 100 && octets[1] is >= 64 and <= 127) &&
               !(octets[0] == 172 && octets[1] is >= 16 and <= 31) &&
               !(octets[0] == 192 && octets[1] == 168) &&
               !(octets[0] == 198 && octets[1] is 18 or 19);
    }
}
