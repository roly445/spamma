namespace Spamma.Modules.UserManagement.Infrastructure.ReadModels;

public class PasskeyLookup
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public byte[] CredentialId { get; init; } = [];

    public string DisplayName { get; init; } = string.Empty;

    public string Algorithm { get; init; } = string.Empty;

    public DateTime RegisteredAt { get; init; }

    public DateTime? LastUsedAt { get; init; }

    public bool IsRevoked { get; init; }

    public DateTime? RevokedAt { get; init; }
}