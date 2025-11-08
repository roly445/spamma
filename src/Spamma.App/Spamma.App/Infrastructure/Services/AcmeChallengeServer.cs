using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Infrastructure.Contracts;

namespace Spamma.App.Infrastructure.Services;

public sealed class AcmeChallengeServer(ILogger<AcmeChallengeServer> logger) : IAcmeChallengeResponder
{
    private readonly ConcurrentDictionary<string, string> _challenges = new();

    public Task RegisterChallengeAsync(string token, string keyAuthorization, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogError("Cannot register challenge with empty token");
            return Task.CompletedTask;
        }

        this._challenges.AddOrUpdate(token, keyAuthorization, (_, _) => keyAuthorization);
        logger.LogInformation("Registered challenge token: {Token}", token);
        return Task.CompletedTask;
    }

    public Task ClearChallengesAsync(CancellationToken cancellationToken = default)
    {
        this._challenges.Clear();
        logger.LogInformation("Cleared all challenge tokens");
        return Task.CompletedTask;
    }

    internal string? GetChallenge(string token)
    {
        if (this._challenges.TryGetValue(token, out var keyAuth))
        {
            logger.LogInformation("Retrieved challenge for token: {Token}", token);
            return keyAuth;
        }

        logger.LogWarning("Challenge token not found: {Token}", token);
        return null;
    }
}