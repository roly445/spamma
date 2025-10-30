namespace Spamma.App.Infrastructure.Endpoints.Setup;

/// <summary>
/// Request model for certificate generation.
/// </summary>
public record GenerateCertificateRequest
{
    /// <summary>
    /// Gets the domain to generate a certificate for.
    /// </summary>
    public required string Domain { get; init; }

    /// <summary>
    /// Gets the email address for the certificate.
    /// </summary>
    public required string Email { get; init; }
}