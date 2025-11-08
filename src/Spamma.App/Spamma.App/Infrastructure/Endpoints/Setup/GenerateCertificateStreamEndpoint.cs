using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common.Infrastructure.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.App.Infrastructure.Endpoints.Setup;

public static class GenerateCertificateStreamEndpoint
{
    public static IEndpointRouteBuilder MapGenerateCertificateStreamEndpoint(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/api/setup/generate-certificate-stream", GenerateCertificateStream)
            .WithName("GenerateCertificateStream")
            .WithDescription("Generate a Let's Encrypt certificate with progress streaming via SSE");

        return routeBuilder;
    }
    private static async Task GenerateCertificateStream(
        GenerateCertificateRequest request,
        HttpContext httpContext,
        ICertesLetsEncryptService certService,
        IAcmeChallengeResponder challengeResponder,
        ILogger<GenerateCertificateRequest> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received certificate generation stream request for domain: {Domain}", request.Domain);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Domain))
        {
            logger.LogWarning("Certificate generation request missing domain");
            httpContext.Response.StatusCode = 400;
            await httpContext.Response.WriteAsync("Domain is required", cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            logger.LogWarning("Certificate generation request missing email");
            httpContext.Response.StatusCode = 400;
            await httpContext.Response.WriteAsync("Email is required", cancellationToken);
            return;
        }

        // Set up SSE response
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.Append("Cache-Control", "no-cache");
        httpContext.Response.Headers.Append("Connection", "keep-alive");

        // Create a progress reporter that writes SSE events
        var progress = new Progress<string>(message =>
        {
            _ = WriteProgressEventAsync(httpContext.Response, message, cancellationToken);
        });

        try
        {
            logger.LogInformation("Generating certificate for domain {Domain} with email {Email}", request.Domain, request.Email);

            // Generate certificate with progress reporting using production Let's Encrypt
            var result = await certService.GenerateCertificateAsync(
                request.Domain,
                request.Email,
                useStaging: false,
                challengeResponder,
                progress,
                cancellationToken);

            if (!result.IsSuccess)
            {
                logger.LogError("Certificate generation failed: {Error}", result.Error);
                await WriteProgressEventAsync(
                    httpContext.Response,
                    $"error:{result.Error}",
                    cancellationToken);
            }
            else
            {
                logger.LogInformation("Certificate generated successfully for domain {Domain}", request.Domain);

                // Save certificate to certs directory (matches CertificateRenewalBackgroundService location)
                var certificatePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "certs",
                    $"{request.Domain}.pfx");

                Directory.CreateDirectory(Path.GetDirectoryName(certificatePath)!);
                await File.WriteAllBytesAsync(certificatePath, result.Value, cancellationToken);

                await WriteProgressEventAsync(
                    httpContext.Response,
                    $"complete:{certificatePath}",
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating certificate for domain: {Domain}", request.Domain);
            await WriteProgressEventAsync(
                httpContext.Response,
                $"error:Certificate generation failed: {ex.Message}",
                cancellationToken);
        }
        finally
        {
            await httpContext.Response.CompleteAsync();
        }
    }
    private static async Task WriteProgressEventAsync(
        HttpResponse response,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await response.WriteAsync($"data: {message}\n\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Client may have disconnected
            System.Diagnostics.Debug.WriteLine($"Failed to write SSE event: {ex.Message}");
        }
    }
}