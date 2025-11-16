namespace Spamma.Modules.UserManagement.Infrastructure.ReadModels;

public class ApiKeyLookup
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string KeyHashPrefix { get; set; } = string.Empty;

    public string KeyHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsRevoked => this.RevokedAt.HasValue;

    public bool IsExpired => this.ExpiresAt.HasValue && DateTimeOffset.UtcNow >= this.ExpiresAt.Value;

    public bool IsActive => !this.IsRevoked && !this.IsExpired;
}