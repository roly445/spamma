using Spamma.App.Infrastructure.Services;

namespace Spamma.App.Infrastructure.Middleware;

/// <summary>
/// Middleware that responds to ACME HTTP-01 challenge requests.
/// Serves challenge responses at /.well-known/acme-challenge/{token}.
/// </summary>
public sealed class AcmeChallengeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AcmeChallengeMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcmeChallengeMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public AcmeChallengeMiddleware(RequestDelegate next, ILogger<AcmeChallengeMiddleware> logger)
    {
        this._next = next;
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="acmeChallengeServer">Service for retrieving challenge tokens.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, AcmeChallengeServer acmeChallengeServer)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Check if this is an ACME challenge request
        if (path.StartsWith("/.well-known/acme-challenge/", StringComparison.OrdinalIgnoreCase))
        {
            var token = path.Substring("/.well-known/acme-challenge/".Length);

            if (string.IsNullOrWhiteSpace(token))
            {
                this._logger.LogWarning("ACME challenge request with empty token");
                context.Response.StatusCode = 404;
                return;
            }

            var keyAuth = acmeChallengeServer.GetChallenge(token);
            if (keyAuth is null)
            {
                this._logger.LogWarning("ACME challenge token not found: {Token}", token);
                context.Response.StatusCode = 404;
                return;
            }

            this._logger.LogInformation("Responding to ACME challenge for token: {Token}", token);
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(keyAuth, context.RequestAborted);
            return;
        }

        await this._next(context);
    }
}