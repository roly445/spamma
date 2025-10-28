using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Infrastructure.ReadModels;

public class UserLookup
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string EmailAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LastPasskeyAuthenticationAt { get; set; }

    public bool IsSuspended { get; set; }

    public DateTime? WhenSuspended { get; set; }

    public SystemRole SystemRole { get; set; }

    public List<Guid> ModeratedDomains { get; set; } = new();

    public List<Guid> ModeratedSubdomains { get; set; } = new();

    public List<Guid> ViewableSubdomains { get; set; } = new();
}