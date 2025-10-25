using Spamma.Modules.DomainManagement.Client.Contracts;
using SubdomainAggregate = Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Builders;

/// <summary>
/// Fluent builder for creating test Subdomain aggregates.
/// Enables readable test setup with sensible defaults.
/// </summary>
public class SubdomainBuilder
{
    private Guid _subdomainId = Guid.NewGuid();
    private Guid _domainId = Guid.NewGuid();
    private string _name = "mail";
    private string? _description = "Test subdomain";
    private DateTime _whenCreated = DateTime.UtcNow;
    private readonly List<(Guid UserId, DateTime WhenAdded)> _moderators = new();
    private readonly List<(Guid UserId, DateTime WhenAdded)> _viewers = new();
    private SubdomainSuspensionReason? _suspensionReason;
    private DateTime? _suspensionDate;
    private readonly List<(MxStatus Status, DateTime When)> _mxChecks = new();

    /// <summary>
    /// Sets the subdomain ID.
    /// </summary>
    public SubdomainBuilder WithId(Guid subdomainId)
    {
        _subdomainId = subdomainId;
        return this;
    }

    /// <summary>
    /// Sets the domain ID that owns this subdomain.
    /// </summary>
    public SubdomainBuilder WithDomainId(Guid domainId)
    {
        _domainId = domainId;
        return this;
    }

    /// <summary>
    /// Sets the subdomain name.
    /// </summary>
    public SubdomainBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the subdomain description.
    /// </summary>
    public SubdomainBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the creation timestamp.
    /// </summary>
    public SubdomainBuilder WithCreatedDate(DateTime whenCreated)
    {
        _whenCreated = whenCreated;
        return this;
    }

    /// <summary>
    /// Adds a moderator to the subdomain.
    /// </summary>
    public SubdomainBuilder WithModerator(Guid userId, DateTime? whenAdded = null)
    {
        _moderators.Add((userId, whenAdded ?? _whenCreated));
        return this;
    }

    /// <summary>
    /// Adds a viewer to the subdomain.
    /// </summary>
    public SubdomainBuilder WithViewer(Guid userId, DateTime? whenAdded = null)
    {
        _viewers.Add((userId, whenAdded ?? _whenCreated));
        return this;
    }

    /// <summary>
    /// Marks the subdomain as suspended.
    /// </summary>
    public SubdomainBuilder WithSuspension(SubdomainSuspensionReason reason, DateTime? suspensionDate = null)
    {
        _suspensionReason = reason;
        _suspensionDate = suspensionDate ?? _whenCreated;
        return this;
    }

    /// <summary>
    /// Logs an MX record check result.
    /// </summary>
    public SubdomainBuilder WithMxCheck(MxStatus status, DateTime? when = null)
    {
        _mxChecks.Add((status, when ?? _whenCreated));
        return this;
    }

    /// <summary>
    /// Builds and returns the Subdomain aggregate.
    /// </summary>
    public SubdomainAggregate Build()
    {
        var result = SubdomainAggregate.Create(_subdomainId, _domainId, _name, _description, _whenCreated);
        var subdomain = result.Value;

        // Apply suspension if specified
        if (_suspensionReason.HasValue)
        {
            subdomain.Suspend(_suspensionReason.Value, null, _suspensionDate ?? _whenCreated);
        }

        // Add moderators
        foreach (var (userId, whenAdded) in _moderators)
        {
            subdomain.AddModerationUser(userId, whenAdded);
        }

        // Add viewers
        foreach (var (userId, whenAdded) in _viewers)
        {
            subdomain.AddViewer(userId, whenAdded);
        }

        // Log MX checks
        foreach (var (status, when) in _mxChecks)
        {
            subdomain.LogMxRecordCheck(status, when);
        }

        return subdomain;
    }
}
