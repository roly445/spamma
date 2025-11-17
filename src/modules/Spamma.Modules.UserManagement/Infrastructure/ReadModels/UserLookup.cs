using Spamma.Modules.Common.Client;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

namespace Spamma.Modules.UserManagement.Infrastructure.ReadModels;

public class UserLookup
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string EmailAddress { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime? LastLoginAt { get; init; }

    public DateTime? LastPasskeyAuthenticationAt { get; init; }

    public bool IsSuspended { get; init; }

    public DateTime? SuspendedAt { get; init; }

    public SystemRole SystemRole { get; init; }

    /// <summary>
    /// Gets the domains moderated by this user. Uses init accessor for immutability; Marten can modify via Patch().Append/Remove.
    /// </summary>
    public List<Guid> ModeratedDomains { get; init; } = new();

    /// <summary>
    /// Gets the subdomains moderated by this user. Uses init accessor for immutability; Marten can modify via Patch().Append/Remove.
    /// </summary>
    public List<Guid> ModeratedSubdomains { get; init; } = new();

    /// <summary>
    /// Gets the subdomains viewable by this user. Uses init accessor for immutability; Marten can modify via Patch().Append/Remove.
    /// </summary>
    public List<Guid> ViewableSubdomains { get; init; } = new();
}