using Spamma.App.Infrastructure.Services;

namespace Spamma.App.Infrastructure.Middleware;

public sealed class AcmeChallengeMiddleware(RequestDelegate next, ILogger<AcmeChallengeMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, AcmeChallengeServer acmeChallengeServer)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Check if this is an ACME challenge request
        if (path.StartsWith("/.well-known/acme-challenge/", StringComparison.OrdinalIgnoreCase))
        {
            var token = path.Substring("/.well-known/acme-challenge/".Length);

            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("ACME challenge request with empty token");
                context.Response.StatusCode = 404;
                return;
            }

            var keyAuth = acmeChallengeServer.GetChallenge(token);
            if (keyAuth is null)
            {
                logger.LogWarning("ACME challenge token not found: {Token}", token);
                context.Response.StatusCode = 404;
                return;
            }

            logger.LogInformation("Responding to ACME challenge for token: {Token}", token);
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(keyAuth, context.RequestAborted);
            return;
        }

        await next(context);
    }
}