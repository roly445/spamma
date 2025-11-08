using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Infrastructure.Contracts;

namespace Spamma.App.Infrastructure.Services;

public sealed class AcmeChallengeServer : IAcmeChallengeResponder
{
    private readonly ILogger<AcmeChallengeServer> _logger;
    private readonly ConcurrentDictionary<string, string> _challenges;
    public AcmeChallengeServer(ILogger<AcmeChallengeServer> logger)
    {
        this._logger = logger;
        this._challenges = new ConcurrentDictionary<string, string>();
    }

    public Task RegisterChallengeAsync(string token, string keyAuthorization, CancellationToken cancellationToken = default)
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

    public Task ClearChallengesAsync(CancellationToken cancellationToken = default)
    {
        this._challenges.Clear();
        this._logger.LogInformation("Cleared all challenge tokens");
        return Task.CompletedTask;
    }
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