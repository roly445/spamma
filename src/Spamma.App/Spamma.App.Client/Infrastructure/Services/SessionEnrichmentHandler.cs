using System.Diagnostics;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.App.Client.Infrastructure.Observability;

namespace Spamma.App.Client.Infrastructure.Services;

public sealed class SessionEnrichmentHandler(IClientSessionContext clientSessionContext) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sessionId = await clientSessionContext.GetSessionIdAsync();
        request.Headers.Remove(SpammaTelemetryContext.SessionHeaderName);
        request.Headers.TryAddWithoutValidation(SpammaTelemetryContext.SessionHeaderName, sessionId);

        if (!string.IsNullOrWhiteSpace(clientSessionContext.CurrentRoute))
        {
            request.Headers.Remove(SpammaTelemetryContext.RouteHeaderName);
            request.Headers.TryAddWithoutValidation(SpammaTelemetryContext.RouteHeaderName, clientSessionContext.CurrentRoute);
        }

        Activity.Current?.SetTag(SpammaTelemetryContext.SessionTagName, sessionId);
        Activity.Current?.SetTag(SpammaTelemetryContext.RouteTagName, clientSessionContext.CurrentRoute);

        return await base.SendAsync(request, cancellationToken);
    }
}
