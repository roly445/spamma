using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Domain.UserAggregate;

namespace Spamma.Modules.UserManagement.Tests.Builders;

public class UserBuilder
{
    private Guid _userId = Guid.NewGuid();
    private string _name = "Test User";
    private string _emailAddress = "test@example.com";
    private Guid _securityStamp = Guid.NewGuid();
    private SystemRole _systemRole = SystemRole.UserManagement;

    public UserBuilder WithId(Guid userId)
    {
        this._userId = userId;
        return this;
    }

    public UserBuilder WithName(string name)
    {
        this._name = name;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        this._emailAddress = email;
        return this;
    }

    public UserBuilder WithSecurityStamp(Guid securityStamp)
    {
        this._securityStamp = securityStamp;
        return this;
    }

    public UserBuilder WithRole(SystemRole role)
    {
        this._systemRole = role;
        return this;
    }

    public User Build()
    {
        var result = User.Create(this._userId, this._name, this._emailAddress, this._securityStamp, DateTime.UtcNow, this._systemRole);
        return result.Value;
    }
}