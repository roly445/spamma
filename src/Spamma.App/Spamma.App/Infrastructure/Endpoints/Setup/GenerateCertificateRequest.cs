namespace Spamma.App.Infrastructure.Endpoints.Setup;

public record GenerateCertificateRequest
{
    public required string Domain { get; init; }

    public required string Email { get; init; }
}