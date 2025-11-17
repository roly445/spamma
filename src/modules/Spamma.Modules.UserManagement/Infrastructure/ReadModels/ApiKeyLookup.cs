namespace Spamma.Modules.UserManagement.Infrastructure.ReadModels;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

public class ApiKeyLookup
{
    public Guid Id { get; internal set; }

    public Guid UserId { get; internal set; }

    public string Name { get; internal set; } = string.Empty;

    public string KeyHashPrefix { get; internal set; } = string.Empty;

    public string KeyHash { get; internal set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset? ExpiresAt { get; internal set; }

    public DateTimeOffset? RevokedAt { get; internal set; }

    public bool IsRevoked => this.RevokedAt.HasValue;

    public bool IsExpired => this.ExpiresAt.HasValue && DateTimeOffset.UtcNow >= this.ExpiresAt.Value;

    public bool IsActive => !this.IsRevoked && !this.IsExpired;
}