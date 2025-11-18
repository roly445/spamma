using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

public class SubdomainLookup
{
    public Guid Id { get; init; }

    public string SubdomainName { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public int AssignedModeratorCount { get; init; }

    public bool IsSuspended { get; init; }

    public DateTime? SuspendedAt { get; init; }

    public string? Description { get; init; }

    public Guid DomainId { get; init; }

    public int ChaosMonkeyRuleCount { get; init; }

    public int ActiveCampaignCount { get; init; }

    public string ParentName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public IReadOnlyList<SubdomainModerator> SubdomainModerators { get; init; } = [];

    public IReadOnlyList<Viewer> Viewers { get; init; } = [];

    public int AssignedViewerCount { get; init; }

    public DateTime? MxLastCheckedAt { get; init; }

    public MxStatus MxStatus { get; init; } = MxStatus.NotChecked;
}