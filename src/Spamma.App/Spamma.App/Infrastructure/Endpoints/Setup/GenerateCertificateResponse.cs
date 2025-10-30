namespace Spamma.App.Infrastructure.Endpoints.Setup;

/// <summary>
/// Response model for certificate generation.
/// </summary>
public record GenerateCertificateResponse
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the domain the certificate was generated for.
    /// </summary>
    public required string Domain { get; init; }

    /// <summary>
    /// Gets the path where the certificate was saved.
    /// </summary>
    public required string CertificatePath { get; init; }

    /// <summary>
    /// Gets the timestamp when the certificate was generated.
    /// </summary>
    public required DateTime GeneratedAt { get; init; }

    /// <summary>
    /// Gets the response message.
    /// </summary>
    public required string Message { get; init; }
}