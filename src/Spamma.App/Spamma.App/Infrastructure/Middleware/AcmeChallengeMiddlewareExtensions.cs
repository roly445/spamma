namespace Spamma.App.Infrastructure.Middleware;

public static class AcmeChallengeMiddlewareExtensions
{
    public static IApplicationBuilder UseAcmeChallenge(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AcmeChallengeMiddleware>();
    }
}