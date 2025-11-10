namespace Spamma.Modules.Common.Infrastructure.Contracts;

public interface IAcmeChallengeResponder
{
    Task RegisterChallengeAsync(string token, string keyAuthorization, CancellationToken cancellationToken = default);

    Task ClearChallengesAsync(CancellationToken cancellationToken = default);
}