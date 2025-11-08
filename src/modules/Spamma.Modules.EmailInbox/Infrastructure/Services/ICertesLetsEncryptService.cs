using Spamma.Modules.Common.Infrastructure.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public interface ICertesLetsEncryptService
{
    Task<ResultMonad.Result<byte[], string>> GenerateCertificateAsync(
        string domain,
        string email,
        bool useStaging,
        IAcmeChallengeResponder challengeResponder,
        IProgress<string>? progressPublisher = null,
        CancellationToken cancellationToken = default);
}