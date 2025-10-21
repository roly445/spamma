using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

public class SubdomainLookup
{
    public Guid Id { get; set; }

    public string SubdomainName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int AssignedModeratorCount { get; set; }

    public bool IsSuspended { get; set; }

    public DateTime? WhenSuspended { get; set; }

    public string? Description { get; set; }

    public Guid DomainId { get; set; }

    public int ChaosMonkeyRuleCount { get; set; }

    public int ActiveCampaignCount { get; set; }

    public string ParentName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public List<SubdomainModerator> SubdomainModerators { get; set; } = new();

    public List<Viewer> Viewers { get; set; } = new();

    public int AssignedViewerCount { get; set; }

    public DateTime? MxLastCheckedAt { get; set; }

    public MxStatus MxStatus { get; set; } = MxStatus.NotChecked;
}