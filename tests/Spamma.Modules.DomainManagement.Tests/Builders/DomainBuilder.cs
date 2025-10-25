using Spamma.Modules.DomainManagement.Client.Contracts;
using DomainAggregate = Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain;

namespace Spamma.Modules.DomainManagement.Tests.Builders;

/// <summary>
/// Fluent builder for creating test Domain aggregates.
/// Enables readable test setup with sensible defaults.
/// </summary>
public class DomainBuilder
{
    private Guid _domainId = Guid.NewGuid();
    private string _name = "example.com";
    private string? _primaryContactEmail = "contact@example.com";
    private string? _description = "Test domain";
    private DateTime _whenCreated = DateTime.UtcNow;
    private readonly List<(Guid UserId, DateTime WhenAdded)> _moderators = new();
    private DomainSuspensionReason? _suspensionReason;
    private DateTime? _suspensionDate;
    private DateTime? _verificationDate;

    /// <summary>
    /// Sets the domain ID.
    /// </summary>
    public DomainBuilder WithId(Guid domainId)
    {
        _domainId = domainId;
        return this;
    }

    /// <summary>
    /// Sets the domain name.
    /// </summary>
    public DomainBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the primary contact email address.
    /// </summary>
    public DomainBuilder WithPrimaryContactEmail(string? email)
    {
        _primaryContactEmail = email;
        return this;
    }

    /// <summary>
    /// Sets the domain description.
    /// </summary>
    public DomainBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the creation timestamp.
    /// </summary>
    public DomainBuilder WithCreatedDate(DateTime whenCreated)
    {
        _whenCreated = whenCreated;
        return this;
    }

    /// <summary>
    /// Adds a moderator to the domain.
    /// </summary>
    public DomainBuilder WithModerator(Guid userId, DateTime? whenAdded = null)
    {
        _moderators.Add((userId, whenAdded ?? _whenCreated));
        return this;
    }

    /// <summary>
    /// Marks the domain as suspended.
    /// </summary>
    public DomainBuilder WithSuspension(DomainSuspensionReason reason, DateTime? suspensionDate = null)
    {
        _suspensionReason = reason;
        _suspensionDate = suspensionDate ?? _whenCreated;
        return this;
    }

    /// <summary>
    /// Marks the domain as verified.
    /// </summary>
    public DomainBuilder WithVerification(DateTime? verificationDate = null)
    {
        _verificationDate = verificationDate ?? _whenCreated;
        return this;
    }

    /// <summary>
    /// Builds and returns the Domain aggregate.
    /// </summary>
    public DomainAggregate Build()
    {
        var result = DomainAggregate.Create(_domainId, _name, _primaryContactEmail, _description, _whenCreated);
        var domain = result.Value;

        // Apply verification if specified
        if (_verificationDate.HasValue)
        {
            domain.MarkAsVerified(_verificationDate.Value);
        }

        // Apply suspension if specified
        if (_suspensionReason.HasValue)
        {
            domain.Suspend(_suspensionReason.Value, null, _suspensionDate ?? _whenCreated);
        }

        // Add moderators
        foreach (var (userId, whenAdded) in _moderators)
        {
            domain.AddModerationUser(userId, whenAdded);
        }

        return domain;
    }
}
