using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Infrastructure.Middleware;

public class SetupModeMiddleware(RequestDelegate next, ILogger<SetupModeMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IInMemorySetupAuthService setupAuth)
    {
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        var isSetupPath = path.StartsWith("/setup");
        var isSetupLoginPath = path == "/setup-login";
        var isSetupMode = setupAuth.IsSetupModeEnabled;

        // Allow static assets and essential endpoints regardless of setup mode
        if (ShouldAllowPath(path))
        {
            await next(context);
            return;
        }

        // If setup mode is disabled, block all setup paths
        if (!isSetupMode && (isSetupPath || isSetupLoginPath))
        {
            logger.LogWarning(
                "Attempt to access setup page after setup completion from {IpAddress}: {Path}",
                context.Connection.RemoteIpAddress, path);
            context.Response.Redirect("/");
            return;
        }

        // If setup mode is enabled and accessing non-setup paths, redirect to login
        if (isSetupMode && !isSetupPath && !isSetupLoginPath)
        {
            context.Response.Redirect("/setup-login");
            return;
        }

        // If accessing setup paths (except login), check authentication
        if (isSetupPath && !isSetupLoginPath)
        {
            var isAuthenticated = context.Session.GetString("SetupAuthenticated") == "true";
            if (!isAuthenticated)
            {
                logger.LogInformation(
                    "Unauthenticated access attempt to {Path} from {IpAddress}",
                    path, context.Connection.RemoteIpAddress);
                context.Response.Redirect("/setup-login");
                return;
            }
        }

        await next(context);
    }

    private static bool ShouldAllowPath(string path)
    {
        // Static assets that should always be allowed
        var allowedPaths = new[]
        {
            // CSS and styles
            "/css/",
            "/styles/",
            "/_content/", // Blazor component assets

            // JavaScript and modules
            "/js/",
            "/scripts/",
            "/_framework/", // Blazor framework files

            // Images and media
            "/images/",
            "/img/",
            "/icons/",
            "/favicon.ico",
            "/apple-touch-icon",
            "/android-chrome-",
            "/mstile-",
            "/safari-pinned-tab.svg",
            "/browserconfig.xml",
            "/site.webmanifest",

            // Fonts
            "/fonts/",
            "/webfonts/",

            // API endpoints (if you have any)
            "/api/",

            // ACME challenge validation (for Let's Encrypt)
            "/.well-known/acme-challenge/",

            // Health checks and monitoring
            "/health",
            "/ping",

            // Vite/build assets (for development)
            "/@vite/",
            "/node_modules/",
            "/@fs/",

            // Common static file extensions
            ".css",
            ".js",
            ".map",
            ".woff",
            ".woff2",
            ".ttf",
            ".eot",
            ".svg",
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".ico",
            ".webp",
            ".avif",
        };

        return allowedPaths.Any(path.Contains);
    }
}