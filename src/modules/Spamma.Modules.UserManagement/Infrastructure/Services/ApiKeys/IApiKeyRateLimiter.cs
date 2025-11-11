namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

/// <summary>
/// Service for rate limiting API key usage.
/// </summary>
public interface IApiKeyRateLimiter
{
    /// <summary>
    /// Checks if the API key is within rate limits.
    /// </summary>
    /// <param name="apiKey">The API key to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if within limits, false if rate limited.</returns>
    Task<bool> IsWithinRateLimitAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a request for the API key (increments the counter).
    /// </summary>
    /// <param name="apiKey">The API key to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordRequestAsync(string apiKey, CancellationToken cancellationToken = default);
}