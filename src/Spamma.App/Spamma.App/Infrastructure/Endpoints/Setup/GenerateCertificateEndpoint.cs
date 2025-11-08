using BluQube.Commands;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.Modules.Common.Infrastructure.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.App.Infrastructure.Endpoints.Setup;

public static class GenerateCertificateEndpoint
{
    public static IEndpointRouteBuilder MapGenerateCertificateEndpoint(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/api/setup/generate-certificate", GenerateCertificate)
            .WithName("GenerateCertificate")
            .WithDescription("Generate a Let's Encrypt certificate using HTTP-01 challenge");

        return routeBuilder;
    }

    private static async Task<IResult> GenerateCertificate(
        GenerateCertificateRequest request,
        ICertesLetsEncryptService certService,
        IAcmeChallengeResponder challengeResponder,
        ILogger<GenerateCertificateRequest> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received certificate generation request for domain: {Domain}", request.Domain);

        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Domain))
            {
                logger.LogWarning("Certificate generation request missing domain");
                return Results.BadRequest(new { error = "Domain is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                logger.LogWarning("Certificate generation request missing email");
                return Results.BadRequest(new { error = "Email is required" });
            }

            logger.LogInformation("Generating certificate for domain {Domain} with email {Email}", request.Domain, request.Email);

            // Generate certificate using production Let's Encrypt
            var result = await certService.GenerateCertificateAsync(
                request.Domain,
                request.Email,
                useStaging: false,
                challengeResponder,
                progressPublisher: null,
                cancellationToken);

            if (!result.IsSuccess)
            {
                logger.LogError("Certificate generation failed: {Error}", result.Error);
                return Results.BadRequest(new { error = result.Error });
            }

            logger.LogInformation("Certificate generated successfully for domain {Domain}", request.Domain);

            // Save certificate to configured location
            var certificatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "certs",
                $"certificate_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss}.pfx");

            var certificateDir = Path.GetDirectoryName(certificatePath)!;
            if (!Directory.Exists(certificateDir))
            {
                Directory.CreateDirectory(certificateDir);
            }

            await File.WriteAllBytesAsync(certificatePath, result.Value, cancellationToken);
            logger.LogInformation("Certificate saved to {Path}", certificatePath);

            return Results.Ok(new GenerateCertificateResponse
            {
                Success = true,
                Domain = request.Domain,
                CertificatePath = certificatePath,
                GeneratedAt = DateTime.UtcNow,
                Message = $"Certificate successfully generated for {request.Domain}",
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception during certificate generation");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}