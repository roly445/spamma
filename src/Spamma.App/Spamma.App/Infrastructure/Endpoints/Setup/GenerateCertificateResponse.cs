namespace Spamma.App.Infrastructure.Endpoints.Setup;

public record GenerateCertificateResponse
{
    public required bool Success { get; init; }

    public required string Domain { get; init; }

    public required string CertificatePath { get; init; }

    public required DateTime GeneratedAt { get; init; }

    public required string Message { get; init; }
}