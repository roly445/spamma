using Certes;
using Certes.Acme;
using Microsoft.Extensions.Logging;
using ResultMonad;
using Spamma.Modules.Common.Infrastructure.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

/// <summary>
/// Implementation of Let's Encrypt certificate generation using Certes.
/// </summary>
internal sealed class CertesLetsEncryptService : ICertesLetsEncryptService
{
    private const string CertificatePassword = "letmein";

    private readonly ILogger<CertesLetsEncryptService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CertesLetsEncryptService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public CertesLetsEncryptService(ILogger<CertesLetsEncryptService> logger)
    {
        this._logger = logger;
    }

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
    public async Task<Result<byte[], string>> GenerateCertificateAsync(
        string domain,
        string email,
        bool useStaging,
        IAcmeChallengeResponder challengeResponder,
        IProgress<string>? progressPublisher = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                this._logger.LogError("Domain cannot be empty");
                return Result.Fail<byte[], string>("Domain is required");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                this._logger.LogError("Email cannot be empty");
                return Result.Fail<byte[], string>("Email is required");
            }

            this._logger.LogInformation("Starting certificate generation for domain: {Domain}", domain);
            progressPublisher?.Report("Step 1 of 6: Creating ACME account...");

            var uri = useStaging ? WellKnownServers.LetsEncryptStagingV2 : WellKnownServers.LetsEncryptV2;
            var acme = new AcmeContext(uri);

            this._logger.LogInformation("Creating or retrieving ACME account for {Email}", email);
            await acme.NewAccount(new[] { $"mailto:{email}" }, true);

            progressPublisher?.Report("Step 2 of 6: Creating order for domain...");
            this._logger.LogInformation("Creating order for domain: {Domain}", domain);
            var order = await acme.NewOrder(new[] { domain });

            progressPublisher?.Report("Step 3 of 6: Setting up HTTP-01 challenge...");
            var authorizations = await order.Authorizations();
            foreach (var authz in authorizations)
            {
                var httpChallenge = await authz.Http();
                var token = httpChallenge.Token;
                var keyAuth = httpChallenge.KeyAuthz;

                this._logger.LogInformation("Registering HTTP-01 challenge for domain: {Domain}, token: {Token}", domain, token);
                await challengeResponder.RegisterChallengeAsync(token, keyAuth, cancellationToken);

                progressPublisher?.Report("Step 4 of 6: Validating domain...");
                this._logger.LogInformation("Answering HTTP-01 challenge for token: {Token}", token);
                await httpChallenge.Validate();

                // Wait for Let's Encrypt to validate the challenge (up to 30 seconds)
                var maxAttempts = 30;
                var attempts = 0;
                while (attempts < maxAttempts)
                {
                    await Task.Delay(1000, cancellationToken);
                    var updated = await authz.Resource();
                    this._logger.LogInformation("Challenge status: {Status}, attempt {Attempt}/{MaxAttempts}", updated.Status, attempts + 1, maxAttempts);

                    // Report progress every few seconds during validation
                    if (attempts % 5 == 0 && attempts > 0)
                    {
                        progressPublisher?.Report($"Step 4 of 6: Validating domain... ({attempts}s elapsed)");
                    }

                    if (updated.Status?.ToString() == "Valid")
                    {
                        this._logger.LogInformation("Challenge validated successfully");
                        break;
                    }

                    if (updated.Status?.ToString() == "Invalid")
                    {
                        this._logger.LogError("Challenge validation failed");
                        return Result.Fail<byte[], string>("Challenge validation failed by Let's Encrypt - ensure domain is publicly accessible and port 80 is open");
                    }

                    attempts++;
                }

                if (attempts >= maxAttempts)
                {
                    this._logger.LogError("Challenge validation timed out after {MaxAttempts} seconds", maxAttempts);
                    return Result.Fail<byte[], string>("Challenge validation timed out - domain may not be publicly accessible");
                }

                this._logger.LogInformation("Challenge answered successfully");
            }

            progressPublisher?.Report("Step 5 of 6: Waiting for order to be ready...");
            this._logger.LogInformation("Waiting for order to be ready for finalization");

            // Wait for order to be ready (status = "Ready")
            var readyAttempts = 0;
            var maxReadyAttempts = 30;
            while (readyAttempts < maxReadyAttempts)
            {
                await Task.Delay(1000, cancellationToken);
                var updatedOrder = await order.Resource();
                this._logger.LogInformation("Order status: {Status}, attempt {Attempt}/{MaxAttempts}", updatedOrder.Status, readyAttempts + 1, maxReadyAttempts);

                // Report progress every few seconds during waiting
                if (readyAttempts % 5 == 0 && readyAttempts > 0)
                {
                    progressPublisher?.Report($"Step 5 of 6: Waiting for order to be ready... ({readyAttempts}s elapsed, this may take up to 30 seconds)");
                }

                if (updatedOrder.Status?.ToString() == "Ready")
                {
                    this._logger.LogInformation("Order is ready for finalization");
                    break;
                }

                if (updatedOrder.Status?.ToString() == "Invalid")
                {
                    this._logger.LogError("Order is invalid - challenge validation may have failed");
                    return Result.Fail<byte[], string>("Order became invalid - challenge validation may have failed");
                }

                readyAttempts++;
            }

            if (readyAttempts >= maxReadyAttempts)
            {
                this._logger.LogError("Order did not become ready after {MaxAttempts} seconds", maxReadyAttempts);
                return Result.Fail<byte[], string>("Order validation timed out - challenges may not have been properly validated");
            }

            // One final refresh to ensure order is truly ready
            progressPublisher?.Report("Step 6 of 6: Finalizing order before certificate generation...");
            await Task.Delay(500, cancellationToken);
            var finalOrderCheck = await order.Resource();
            this._logger.LogInformation("Final order status before Generate: {Status}", finalOrderCheck.Status);
            if (finalOrderCheck.Status?.ToString() != "Ready")
            {
                this._logger.LogError("Order is not ready after all retries. Current status: {Status}", finalOrderCheck.Status);
                return Result.Fail<byte[], string>($"Order not ready for generation (status: {finalOrderCheck.Status})");
            }

            progressPublisher?.Report("Step 6 of 6: Generating certificate...");
            this._logger.LogInformation("Generating private key and certificate signing request");
            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var cert = await order.Generate(
                new CsrInfo
                {
                    CountryName = "US",
                    State = "CA",
                    Locality = "San Francisco",
                    Organization = "Spamma",
                    OrganizationUnit = "Email Service",
                    CommonName = domain,
                },
                privateKey);

            progressPublisher?.Report("Step 6 of 6: Finalizing certificate...");
            progressPublisher?.Report("Step 6 of 6: Finalizing certificate...");
            this._logger.LogInformation("Generating PFX certificate");

            byte[]? pfxBytes = null;

            try
            {
                var pfxBuilder = cert.ToPfx(privateKey);
                pfxBytes = pfxBuilder.Build(domain, CertificatePassword);
                this._logger.LogInformation("PFX generated successfully");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to generate PFX certificate for domain: {Domain}", domain);
                throw new InvalidOperationException($"Failed to generate PFX certificate for domain {domain}", ex);
            }

            if (pfxBytes == null)
            {
                this._logger.LogError("PFX bytes are null after generation");
                return Result.Fail<byte[], string>("Failed to generate PFX certificate");
            }

            progressPublisher?.Report("complete:Certificate generated successfully!");
            this._logger.LogInformation("Certificate generated successfully for domain: {Domain}", domain);
            await challengeResponder.ClearChallengesAsync(cancellationToken);
            return Result.Ok<byte[], string>(pfxBytes);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error generating certificate for domain: {Domain}", domain);
            return Result.Fail<byte[], string>($"Certificate generation failed: {ex.Message}");
        }
    }
}