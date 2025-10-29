using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Infrastructure.Contracts;

namespace Spamma.App.Infrastructure.Services;

/// <summary>
/// HTTP server for responding to ACME HTTP-01 challenges.
/// Implements IAcmeChallengeResponder to store and serve challenge tokens.
/// </summary>
public sealed class AcmeChallengeServer : IAcmeChallengeResponder
{
    private readonly ILogger<AcmeChallengeServer> _logger;
    private readonly ConcurrentDictionary<string, string> _challenges;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcmeChallengeServer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public AcmeChallengeServer(ILogger<AcmeChallengeServer> logger)
    {
        this._logger = logger;
        this._challenges = new ConcurrentDictionary<string, string>();
    }

    /// <summary>
    /// Registers a challenge token-key authorization pair for HTTP-01 validation.
    /// </summary>
    /// <param name="token">The ACME challenge token.</param>
    /// <param name="keyAuthorization">The key authorization string for the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RegisterChallengeAsync(string token, string keyAuthorization, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            this._logger.LogError("Cannot register challenge with empty token");
            return Task.CompletedTask;
        }

        this._challenges.AddOrUpdate(token, keyAuthorization, (_, _) => keyAuthorization);
        this._logger.LogInformation("Registered challenge token: {Token}", token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all registered challenge tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ClearChallengesAsync(CancellationToken cancellationToken)
    {
        this._challenges.Clear();
        this._logger.LogInformation("Cleared all challenge tokens");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the key authorization for a challenge token, if it exists.
    /// </summary>
    /// <param name="token">The challenge token to look up.</param>
    /// <returns>The key authorization string, or null if token not found.</returns>
    internal string? GetChallenge(string token)
    {
        if (this._challenges.TryGetValue(token, out var keyAuth))
        {
            this._logger.LogInformation("Retrieved challenge for token: {Token}", token);
            return keyAuth;
        }

        this._logger.LogWarning("Challenge token not found: {Token}", token);
        return null;
    }
}

