using System.Net;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public static class LocalDomainDetector
{
    public static bool IsLocalDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        if (domain.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (domain.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            domain.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IPAddress.TryParse(domain, out var ipAddress))
        {
            return IPAddress.IsLoopback(ipAddress);
        }

        return false;
    }
}
