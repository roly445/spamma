using Spamma.Modules.Common.Client;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

namespace Spamma.Modules.UserManagement.Infrastructure.ReadModels;

public class UserLookup
{
    public Guid Id { get; internal set; }

    public string Name { get; internal set; } = string.Empty;

    public string EmailAddress { get; internal set; } = string.Empty;

    public DateTime CreatedAt { get; internal set; }

    public DateTime? LastLoginAt { get; internal set; }

    public DateTime? LastPasskeyAuthenticationAt { get; internal set; }

    public bool IsSuspended { get; internal set; }

    public DateTime? SuspendedAt { get; internal set; }

    public SystemRole SystemRole { get; internal set; }

    public List<Guid> ModeratedDomains { get; } = new();

    public List<Guid> ModeratedSubdomains { get; } = new();

    public List<Guid> ViewableSubdomains { get; } = new();
}