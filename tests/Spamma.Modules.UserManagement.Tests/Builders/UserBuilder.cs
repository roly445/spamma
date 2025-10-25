using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.UserAggregate;

namespace Spamma.Modules.UserManagement.Tests.Builders;

/// <summary>
/// Fluent builder for creating test User aggregates.
/// Enables readable test setup with sensible defaults.
/// </summary>
public class UserBuilder
{
    private Guid _userId = Guid.NewGuid();
    private string _name = "Test User";
    private string _emailAddress = "test@example.com";
    private Guid _securityStamp = Guid.NewGuid();
    private SystemRole _systemRole = SystemRole.UserManagement;

    /// <summary>
    /// Sets the user ID.
    /// </summary>
    public UserBuilder WithId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    /// <summary>
    /// Sets the user's name.
    /// </summary>
    public UserBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the user's email address.
    /// </summary>
    public UserBuilder WithEmail(string email)
    {
        _emailAddress = email;
        return this;
    }

    /// <summary>
    /// Sets the user's security stamp (used for authentication).
    /// </summary>
    public UserBuilder WithSecurityStamp(Guid securityStamp)
    {
        _securityStamp = securityStamp;
        return this;
    }

    /// <summary>
    /// Sets the user's system role.
    /// </summary>
    public UserBuilder WithRole(SystemRole role)
    {
        _systemRole = role;
        return this;
    }

    /// <summary>
    /// Builds and returns the User aggregate.
    /// </summary>
    public User Build()
    {
        var result = User.Create(_userId, _name, _emailAddress, _securityStamp, DateTime.UtcNow, _systemRole);
        return result.Value;
    }
}

/// <summary>
/// Extension methods for UserBuilder to support common test scenarios.
/// </summary>
public static class UserBuilderExtensions
{
    /// <summary>
    /// Creates a builder for a suspended user.
    /// </summary>
    public static User BuildSuspendedUser()
    {
        var user = new UserBuilder().Build();
        user.Suspend(AccountSuspensionReason.Administrative, "Test suspension", DateTime.UtcNow);
        return user;
    }

    /// <summary>
    /// Creates a builder for a user with domain management role.
    /// </summary>
    public static User BuildDomainManagementUser()
    {
        return new UserBuilder()
            .WithRole(SystemRole.DomainManagement)
            .Build();
    }

    /// <summary>
    /// Creates a builder for a user with specific ID and email (common for repository tests).
    /// </summary>
    public static User BuildUserWithIdAndEmail(Guid userId, string email)
    {
        return new UserBuilder()
            .WithId(userId)
            .WithEmail(email)
            .Build();
    }
}
