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

    public IReadOnlyList<Guid> ModeratedDomains { get; init; } = [];

    public IReadOnlyList<Guid> ModeratedSubdomains { get; init; } = [];

    public IReadOnlyList<Guid> ViewableSubdomains { get; init; } = [];
}