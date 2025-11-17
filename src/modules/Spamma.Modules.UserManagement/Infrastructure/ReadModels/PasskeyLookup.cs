namespace Spamma.Modules.UserManagement.Infrastructure.ReadModels;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

public class PasskeyLookup
{
    public Guid Id { get; internal set; }

    public Guid UserId { get; internal set; }

    public byte[] CredentialId { get; internal set; } = [];

    public string DisplayName { get; internal set; } = string.Empty;

    public string Algorithm { get; internal set; } = string.Empty;

    public DateTime RegisteredAt { get; internal set; }

    public DateTime? LastUsedAt { get; internal set; }

    public bool IsRevoked { get; internal set; }

    public DateTime? RevokedAt { get; internal set; }
}