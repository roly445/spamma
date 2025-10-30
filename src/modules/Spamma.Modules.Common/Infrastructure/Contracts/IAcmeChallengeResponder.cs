namespace Spamma.Modules.Common.Infrastructure.Contracts;

/// <summary>
/// Abstraction for responding to ACME HTTP-01 challenge requests.
/// </summary>
public interface IAcmeChallengeResponder
{
    /// <summary>
    /// Registers a challenge token-key authorization pair for HTTP-01 validation.
    /// </summary>
    /// <param name="token">The ACME challenge token.</param>
    /// <param name="keyAuthorization">The key authorization string for the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RegisterChallengeAsync(string token, string keyAuthorization, CancellationToken cancellationToken);

    /// <summary>
    /// Clears all registered challenge tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearChallengesAsync(CancellationToken cancellationToken);
}