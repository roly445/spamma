using Spamma.Modules.Common.Infrastructure.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

/// <summary>
/// Service for generating Let's Encrypt certificates using Certes ACME client with HTTP-01 challenge.
/// </summary>
public interface ICertesLetsEncryptService
{
    /// <summary>
    /// Generates a Let's Encrypt certificate for the specified domain.
    /// </summary>
    /// <param name="domain">The domain to generate a certificate for.</param>
    /// <param name="email">Email address for Let's Encrypt account.</param>
    /// <param name="useStaging">Whether to use Let's Encrypt staging server (for testing).</param>
    /// <param name="challengeResponder">Service to register challenge responses.</param>
    /// <param name="progressPublisher">Optional service for publishing progress events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Certificate bytes in PFX format, or error message if generation failed.</returns>
    Task<ResultMonad.Result<byte[], string>> GenerateCertificateAsync(
        string domain,
        string email,
        bool useStaging,
        IAcmeChallengeResponder challengeResponder,
        IProgress<string>? progressPublisher = null,
        CancellationToken cancellationToken = default);
}