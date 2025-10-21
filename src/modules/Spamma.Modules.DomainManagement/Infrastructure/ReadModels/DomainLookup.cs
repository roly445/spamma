namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

public class DomainLookup
{
    public Guid Id { get; set; }

    public string DomainName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string VerificationToken { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public int SubdomainCount { get; set; }

    public int AssignedModeratorCount { get; set; }

    public string? PrimaryContact { get; set; }

    public bool IsSuspended { get; set; }

    public DateTime? WhenSuspended { get; set; }

    public List<SubdomainLookup> Subdomains { get; set; } = new();

    public List<DomainModerator> DomainModerators { get; set; } = new();
}