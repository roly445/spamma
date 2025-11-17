namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

public class DomainLookup
{
    public Guid Id { get; internal set; }

    public string DomainName { get; internal set; } = string.Empty;

    public string? Description { get; internal set; }

    public string VerificationToken { get; internal set; } = string.Empty;

    public bool IsVerified { get; internal set; }

    public DateTime CreatedAt { get; internal set; }

    public DateTime? VerifiedAt { get; internal set; }

    public int SubdomainCount { get; internal set; }

    public int AssignedModeratorCount { get; internal set; }

    public string? PrimaryContact { get; internal set; }

    public bool IsSuspended { get; internal set; }

    public DateTime? SuspendedAt { get; internal set; }

    public List<SubdomainLookup> Subdomains { get; } = new();

    public List<DomainModerator> DomainModerators { get; } = new();
}