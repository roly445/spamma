namespace Spamma.Modules.UserManagement.Infrastructure.ReadModels;

public class PasskeyLookup
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public byte[] CredentialId { get; set; } = [];

    public string DisplayName { get; set; } = string.Empty;

    public string Algorithm { get; set; } = string.Empty;

    public DateTime RegisteredAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }
}