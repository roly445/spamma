using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.App.Client.Infrastructure.Observability;

namespace Spamma.App.Client.Infrastructure.Services;

public sealed class ClientSessionContext(
    IJSRuntime jsRuntime,
    NavigationManager navigationManager,
    ILogger<ClientSessionContext> logger) : IClientSessionContext, IDisposable
{
    private const int MaxBreadcrumbs = 50;
    private static readonly ActivitySource ActivitySource = new("Spamma.App.Client.Session");

    private bool _initialized;
    private bool _disposed;
    private string? _sessionId;
    private int _breadcrumbSequence;

    public string CurrentRoute { get; private set; } = "/";

    public async Task InitializeAsync()
    {
        if (this._initialized)
        {
            return;
        }

        this._sessionId = await this.LoadOrCreateSessionIdAsync(this._sessionId);
        this.CurrentRoute = this.GetCurrentRoute();
        navigationManager.LocationChanged += this.OnLocationChanged;
        this._initialized = true;

        await this.TrackBreadcrumbAsync(
            "session.initialized",
            new Dictionary<string, string?>
            {
                ["route"] = this.CurrentRoute,
            });
    }

    public async Task<string> GetSessionIdAsync()
    {
        if (!string.IsNullOrWhiteSpace(this._sessionId))
        {
            return this._sessionId;
        }

        this._sessionId = await this.LoadOrCreateSessionIdAsync(null);
        this.CurrentRoute = this.GetCurrentRoute();
        return this._sessionId;
    }

    public async Task TrackBreadcrumbAsync(string name, Dictionary<string, string?>? data = null)
    {
        var sessionId = await this.GetSessionIdAsync();
        var breadcrumb = new BrowserBreadcrumb(
            ++this._breadcrumbSequence,
            name,
            DateTimeOffset.UtcNow,
            this.CurrentRoute,
            data);

        using var activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
        activity?.SetTag(SpammaTelemetryContext.SessionTagName, sessionId);
        activity?.SetTag(SpammaTelemetryContext.RouteTagName, this.CurrentRoute);
        activity?.SetTag(SpammaTelemetryContext.BreadcrumbNameTag, name);

        if (data != null)
        {
            foreach (var pair in data.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)))
            {
                activity?.SetTag($"spamma.breadcrumb.{pair.Key}", pair.Value);
            }
        }

        try
        {
            await jsRuntime.InvokeVoidAsync("SpammaSession.appendBreadcrumb", breadcrumb, MaxBreadcrumbs);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to append breadcrumb '{BreadcrumbName}' to session storage", name);
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    private static string CreateFallbackSessionId()
        => Guid.NewGuid().ToString("n");

    private void Dispose(bool disposing)
    {
        if (this._disposed)
        {
            return;
        }

        if (disposing)
        {
            navigationManager.LocationChanged -= this.OnLocationChanged;
        }

        this._disposed = true;
    }

    private string GetCurrentRoute()
    {
        var relativePath = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return "/";
        }

        return relativePath.StartsWith("/", StringComparison.Ordinal) ? relativePath : $"/{relativePath}";
    }

    private async Task<string> LoadOrCreateSessionIdAsync(string? preferredSessionId)
    {
        try
        {
            return await jsRuntime.InvokeAsync<string>("SpammaSession.getOrCreateSessionId", preferredSessionId);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Falling back to an in-memory session identifier");
            return preferredSessionId ?? CreateFallbackSessionId();
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        this.CurrentRoute = this.GetCurrentRoute();
        _ = this.TrackBreadcrumbAsync(
            "route.changed",
            new Dictionary<string, string?>
            {
                ["route"] = this.CurrentRoute,
                ["intercepted"] = e.IsNavigationIntercepted.ToString(),
            });
    }

    private sealed record BrowserBreadcrumb(
        int Sequence,
        string Name,
        DateTimeOffset Timestamp,
        string Route,
        IReadOnlyDictionary<string, string?>? Data);
}
