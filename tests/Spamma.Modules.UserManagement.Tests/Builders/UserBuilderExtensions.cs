using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.UserAggregate;

namespace Spamma.Modules.UserManagement.Tests.Builders;

public static class UserBuilderExtensions
{
    public static User BuildSuspendedUser()
    {
        var user = new UserBuilder().Build();
        user.Suspend(AccountSuspensionReason.Administrative, "Test suspension", DateTime.UtcNow);
        return user;
    }

    public static User BuildDomainManagementUser()
    {
        return new UserBuilder()
            .WithRole(SystemRole.DomainManagement)
            .Build();
    }

    public static User BuildUserWithIdAndEmail(Guid userId, string email)
    {
        return new UserBuilder()
            .WithId(userId)
            .WithEmail(email)
            .Build();
    }
}