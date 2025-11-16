namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

public interface IApiKeyRateLimiter
{
    Task<bool> IsWithinRateLimitAsync(string apiKey, CancellationToken cancellationToken = default);

    Task RecordRequestAsync(string apiKey, CancellationToken cancellationToken = default);
}