namespace Spamma.App.Client.Infrastructure.Observability;

public static class SpammaTelemetryContext
{
    public const string SessionHeaderName = "X-Spamma-Session-Id";
    public const string RouteHeaderName = "X-Spamma-Client-Route";
    public const string SignalRSessionQueryParameterName = "spammaSessionId";
    public const string SessionStorageKey = "spamma.session.id";
    public const string BreadcrumbStorageKey = "spamma.breadcrumbs";
    public const string SessionTagName = "spamma.session_id";
    public const string RouteTagName = "spamma.client_route";
    public const string BreadcrumbNameTag = "spamma.breadcrumb_name";
}
