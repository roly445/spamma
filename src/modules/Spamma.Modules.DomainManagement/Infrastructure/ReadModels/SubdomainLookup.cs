using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

public class SubdomainLookup
{
    public Guid Id { get; internal set; }

    public string SubdomainName { get; internal set; } = string.Empty;

    public DateTime CreatedAt { get; internal set; }

    public int AssignedModeratorCount { get; internal set; }

    public bool IsSuspended { get; internal set; }

    public DateTime? SuspendedAt { get; internal set; }

    public string? Description { get; internal set; }

    public Guid DomainId { get; internal set; }

    public int ChaosMonkeyRuleCount { get; internal set; }

    public int ActiveCampaignCount { get; internal set; }

    public string ParentName { get; internal set; } = string.Empty;

    public string FullName { get; internal set; } = string.Empty;

    public List<SubdomainModerator> SubdomainModerators { get; } = new();

    public List<Viewer> Viewers { get; } = new();

    public int AssignedViewerCount { get; internal set; }

    public DateTime? MxLastCheckedAt { get; internal set; }

    public MxStatus MxStatus { get; private set; } = MxStatus.NotChecked;
}