namespace Spamma.Modules.UserManagement.Infrastructure.ReadModels;

public class ApiKeyLookup
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string KeyHashPrefix { get; init; } = string.Empty;

    public string KeyHash { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public DateTimeOffset? RevokedAt { get; init; }

    public bool IsRevoked => this.RevokedAt.HasValue;

    public bool IsExpired => this.ExpiresAt.HasValue && DateTimeOffset.UtcNow >= this.ExpiresAt.Value;

    public bool IsActive => !this.IsRevoked && !this.IsExpired;
}