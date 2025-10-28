using System.Text.Json.Serialization;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Infrastructure.Auth;

public class UserAuthInfo
{
    [JsonConstructor]
    private UserAuthInfo(string? userId, string? name, string? emailAddress, SystemRole systemRole, IReadOnlyList<Guid> moderatedDomains, IReadOnlyList<Guid> moderatedSubdomains, IReadOnlyList<Guid> viewableSubdomains, bool isAuthenticated)
    {
        this.UserId = userId;
        this.Name = name;
        this.EmailAddress = emailAddress;
        this.SystemRole = systemRole;
        this.ModeratedDomains = moderatedDomains;
        this.ModeratedSubdomains = moderatedSubdomains;
        this.ViewableSubdomains = viewableSubdomains;
        this.IsAuthenticated = isAuthenticated;
    }

    public string? UserId { get; private set; }

    public string? Name { get; private set; }

    public string? EmailAddress { get; private set; }

    public SystemRole SystemRole { get; private set; }

    public IReadOnlyList<Guid> ModeratedDomains { get; private set; }

    public IReadOnlyList<Guid> ModeratedSubdomains { get; private set; }

    public IReadOnlyList<Guid> ViewableSubdomains { get; private set; }

    public bool IsAuthenticated { get; private set; }

    public static UserAuthInfo Authenticated(string userId, string name, string emailAddress, SystemRole systemRole,
        IReadOnlyList<Guid> moderatedDomains, IReadOnlyList<Guid> moderatedSubdomains,
        IReadOnlyList<Guid> viewableSubdomains)
    {
        return new UserAuthInfo(userId, name, emailAddress, systemRole, moderatedDomains, moderatedSubdomains, viewableSubdomains, true);
    }

    public static UserAuthInfo Unauthenticated()
    {
        return new UserAuthInfo(null, null, null, 0, [], [], [], false);
    }
}