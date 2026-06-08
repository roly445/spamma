using System.Diagnostics;
using Spamma.App.Client.Infrastructure.Observability;

namespace Spamma.App.Infrastructure.Middleware;

public sealed class SessionContextEnrichmentMiddleware(RequestDelegate next, ILogger<SessionContextEnrichmentMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sessionId = ResolveSessionId(context);
        var clientRoute = ResolveClientRoute(context);
        var scopeState = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            scopeState["SpammaSessionId"] = sessionId;
            Activity.Current?.SetTag(SpammaTelemetryContext.SessionTagName, sessionId);
        }

        if (!string.IsNullOrWhiteSpace(clientRoute))
        {
            scopeState["SpammaClientRoute"] = clientRoute;
            Activity.Current?.SetTag(SpammaTelemetryContext.RouteTagName, clientRoute);
        }

        if (scopeState.Count == 0)
        {
            await next(context);
            return;
        }

        using (logger.BeginScope(scopeState))
        {
            await next(context);
        }
    }

    private static string? ResolveClientRoute(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(SpammaTelemetryContext.RouteHeaderName, out var clientRoute))
        {
            return clientRoute.ToString();
        }

        return null;
    }

    private static string? ResolveSessionId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(SpammaTelemetryContext.SessionHeaderName, out var sessionId))
        {
            return sessionId.ToString();
        }

        if (context.Request.Query.TryGetValue(SpammaTelemetryContext.SignalRSessionQueryParameterName, out var signalRSessionId))
        {
            return signalRSessionId.ToString();
        }

        return null;
    }
}
