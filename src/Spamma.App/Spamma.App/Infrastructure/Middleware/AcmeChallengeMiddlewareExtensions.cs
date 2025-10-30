namespace Spamma.App.Infrastructure.Middleware;

/// <summary>
/// Extension methods for registering ACME challenge middleware.
/// </summary>
public static class AcmeChallengeMiddlewareExtensions
{
    /// <summary>
    /// Adds ACME challenge middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseAcmeChallenge(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AcmeChallengeMiddleware>();
    }
}