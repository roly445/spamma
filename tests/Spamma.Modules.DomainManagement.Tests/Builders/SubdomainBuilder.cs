using Spamma.Modules.DomainManagement.Client.Contracts;
using SubdomainAggregate = Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Builders;

public class SubdomainBuilder
{
    private readonly List<(Guid UserId, DateTime WhenAdded)> _moderators = new();
    private readonly List<(Guid UserId, DateTime WhenAdded)> _viewers = new();
    private readonly List<(MxStatus Status, DateTime When)> _mxChecks = new();
    private Guid _subdomainId = Guid.NewGuid();
    private Guid _domainId = Guid.NewGuid();
    private string _name = "mail";
    private string? _description = "Test subdomain";
    private DateTime _whenCreated = DateTime.UtcNow;
    private SubdomainSuspensionReason? _suspensionReason;
    private DateTime? _suspensionDate;

    public SubdomainBuilder WithId(Guid subdomainId)
    {
        this._subdomainId = subdomainId;
        return this;
    }

    public SubdomainBuilder WithDomainId(Guid domainId)
    {
        this._domainId = domainId;
        return this;
    }

    public SubdomainBuilder WithName(string name)
    {
        this._name = name;
        return this;
    }

    public SubdomainBuilder WithDescription(string? description)
    {
        this._description = description;
        return this;
    }

    public SubdomainBuilder WithCreatedDate(DateTime whenCreated)
    {
        this._whenCreated = whenCreated;
        return this;
    }

    public SubdomainBuilder WithModerator(Guid userId, DateTime? whenAdded = null)
    {
        this._moderators.Add((userId, whenAdded ?? this._whenCreated));
        return this;
    }

    public SubdomainBuilder WithViewer(Guid userId, DateTime? whenAdded = null)
    {
        this._viewers.Add((userId, whenAdded ?? this._whenCreated));
        return this;
    }

    public SubdomainBuilder WithSuspension(SubdomainSuspensionReason reason, DateTime? suspensionDate = null)
    {
        this._suspensionReason = reason;
        this._suspensionDate = suspensionDate ?? this._whenCreated;
        return this;
    }

    public SubdomainBuilder WithMxCheck(MxStatus status, DateTime? when = null)
    {
        this._mxChecks.Add((status, when ?? this._whenCreated));
        return this;
    }

    internal SubdomainAggregate Build()
    {
        var result = SubdomainAggregate.Create(this._subdomainId, this._domainId, this._name, this._description, this._whenCreated);
        var subdomain = result.Value;

        if (this._suspensionReason.HasValue)
        {
            subdomain.Suspend(this._suspensionReason.Value, null, this._suspensionDate ?? this._whenCreated);
        }

        foreach (var (userId, whenAdded) in this._moderators)
        {
            subdomain.AddModerationUser(userId, whenAdded);
        }

        foreach (var (userId, whenAdded) in this._viewers)
        {
            subdomain.AddViewer(userId, whenAdded);
        }

        foreach (var (status, when) in this._mxChecks)
        {
            subdomain.LogMxRecordCheck(status, when);
        }

        return subdomain;
    }
}