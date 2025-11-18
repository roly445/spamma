namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

public class DomainLookup
{
    public Guid Id { get; init; }

    public string DomainName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string VerificationToken { get; init; } = string.Empty;

    public bool IsVerified { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? VerifiedAt { get; init; }

    public int SubdomainCount { get; init; }

    public int AssignedModeratorCount { get; init; }

    public string? PrimaryContact { get; init; }

    public bool IsSuspended { get; init; }

    public DateTime? SuspendedAt { get; init; }

    public IReadOnlyList<SubdomainLookup> Subdomains { get; init; } = [];

    public IReadOnlyList<DomainModerator> DomainModerators { get; init; } = [];
}