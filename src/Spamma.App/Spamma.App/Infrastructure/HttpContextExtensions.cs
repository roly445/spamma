using System.Net;

namespace Spamma.App.Infrastructure;

public static class HttpContextExtensions
{
    public static bool IsLocal(this HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return false;
        }

        var connection = httpContext.Connection;
        var remoteIp = connection.RemoteIpAddress;
        var localIp = connection.LocalIpAddress;

        if (remoteIp != null)
        {
            // Direct loopback (127.0.0.1 or ::1)
            if (IPAddress.IsLoopback(remoteIp))
            {
                return true;
            }

            // Remote equals local (same machine)
            if (localIp != null && remoteIp.Equals(localIp))
            {
                return true;
            }
        }

        // If behind a proxy and X-Forwarded-For is present, check its first entry
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var header))
        {
            var first = header.ToString().Split(',').Select(h => h.Trim()).FirstOrDefault();
            if (IPAddress.TryParse(first, out var forwardedIp) && IPAddress.IsLoopback(forwardedIp))
            {
                return true;
            }
        }

        return false;
    }
}