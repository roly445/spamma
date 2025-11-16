using Spamma.Modules.DomainManagement.Client.Contracts;
using DomainAggregate = Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain;

namespace Spamma.Modules.DomainManagement.Tests.Builders;

public class DomainBuilder
{
    private readonly List<(Guid UserId, DateTime WhenAdded)> _moderators = new();
    private Guid _domainId = Guid.NewGuid();
    private string _name = "example.com";
    private string? _primaryContactEmail = "contact@example.com";
    private string? _description = "Test domain";
    private DateTime _whenCreated = DateTime.UtcNow;
    private DomainSuspensionReason? _suspensionReason;
    private DateTime? _suspensionDate;
    private DateTime? _verificationDate;

    public DomainBuilder WithId(Guid domainId)
    {
        this._domainId = domainId;
        return this;
    }

    public DomainBuilder WithName(string name)
    {
        this._name = name;
        return this;
    }

    public DomainBuilder WithPrimaryContactEmail(string? email)
    {
        this._primaryContactEmail = email;
        return this;
    }

    public DomainBuilder WithDescription(string? description)
    {
        this._description = description;
        return this;
    }

    public DomainBuilder WithCreatedDate(DateTime whenCreated)
    {
        this._whenCreated = whenCreated;
        return this;
    }

    public DomainBuilder WithModerator(Guid userId, DateTime? whenAdded = null)
    {
        this._moderators.Add((userId, whenAdded ?? this._whenCreated));
        return this;
    }

    public DomainBuilder WithSuspension(DomainSuspensionReason reason, DateTime? suspensionDate = null)
    {
        this._suspensionReason = reason;
        this._suspensionDate = suspensionDate ?? this._whenCreated;
        return this;
    }

    public DomainBuilder WithVerification(DateTime? verificationDate = null)
    {
        this._verificationDate = verificationDate ?? this._whenCreated;
        return this;
    }

    internal DomainAggregate Build()
    {
        var result = DomainAggregate.Create(this._domainId, this._name, this._primaryContactEmail, this._description, this._whenCreated);
        var domain = result.Value;

        if (this._verificationDate.HasValue)
        {
            domain.MarkAsVerified(this._verificationDate.Value);
        }

        if (this._suspensionReason.HasValue)
        {
            domain.Suspend(this._suspensionReason.Value, null, this._suspensionDate ?? this._whenCreated);
        }

        foreach (var (userId, whenAdded) in this._moderators)
        {
            domain.AddModerationUser(userId, whenAdded);
        }

        return domain;
    }
}