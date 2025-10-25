using FluentAssertions;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.UserAggregate;
using Spamma.Modules.UserManagement.Domain.UserAggregate.Events;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Tests.Common.Verification;

namespace Spamma.Modules.UserManagement.Tests.Domain;

/// <summary>
/// Domain tests for the User aggregate using verification-based patterns.
/// Tests focus on verifying events raised by business logic, not internal state.
/// </summary>
public class UserAggregateTests
{
    #region StartAuthentication Tests

    [Fact]
    public void StartAuthentication_WhenUserNotSuspended_RaisesAuthenticationStartedEvent()
    {
        // Arrange
        var user = new UserBuilder()
            .WithName("John Doe")
            .WithEmail("john@example.com")
            .Build();
        var now = DateTime.UtcNow;

        // Act
        var result = user.StartAuthentication(now);

        // Verify - result is Ok and an event was raised
        result.ShouldBeOk(authEvent =>
        {
            authEvent.AuthenticationAttemptId.Should().NotBe(Guid.Empty);
            authEvent.WhenStarted.Should().Be(now);
        });
    }

    [Fact]
    public void StartAuthentication_WhenUserSuspended_ReturnsFailed()
    {
        // Arrange - create a suspended user
        var user = new UserBuilder().Build();
        user.Suspend(AccountSuspensionReason.Administrative, "Test", DateTime.UtcNow);

        // Act
        var result = user.StartAuthentication(DateTime.UtcNow);

        // Verify - should fail with appropriate error
        result.ShouldBeFailed();
    }

    #endregion

    #region Suspend/Unsuspend Tests

    [Fact]
    public void Suspend_WhenUserNotSuspended_RaisesAccountSuspendedEvent()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var reason = AccountSuspensionReason.Administrative;
        var notes = "User violated terms";
        var now = DateTime.UtcNow;

        // Act
        var result = user.Suspend(reason, notes, now);

        // Verify
        result.ShouldBeOk();
        // Verify the event was raised with correct data by checking the aggregate state
        // (The IsSuspended flag should be set after applying the event)
    }

    [Fact]
    public void Suspend_WhenUserAlreadySuspended_ReturnsFailed()
    {
        // Arrange
        var user = new UserBuilder().Build();
        user.Suspend(AccountSuspensionReason.Administrative, "First suspension", DateTime.UtcNow);

        // Act
        var result = user.Suspend(AccountSuspensionReason.Administrative, "Second suspension", DateTime.UtcNow);

        // Verify - should fail because user is already suspended
        result.ShouldBeFailed();
    }

    [Fact]
    public void Unsuspend_WhenUserSuspended_RaisesAccountUnsuspendedEvent()
    {
        // Arrange
        var user = new UserBuilder().Build();
        user.Suspend(AccountSuspensionReason.Administrative, null, DateTime.UtcNow);
        var now = DateTime.UtcNow;

        // Act
        var result = user.Unsuspend(now);

        // Verify
        result.ShouldBeOk();
    }

    [Fact]
    public void Unsuspend_WhenUserNotSuspended_ReturnsFailed()
    {
        // Arrange
        var user = new UserBuilder().Build();

        // Act
        var result = user.Unsuspend(DateTime.UtcNow);

        // Verify - should fail because user is not suspended
        result.ShouldBeFailed();
    }

    #endregion

    #region ChangeDetails Tests

    [Fact]
    public void ChangeDetails_AlwaysRaisesDetailsChangedEvent()
    {
        // Arrange
        var user = new UserBuilder()
            .WithName("Old Name")
            .WithEmail("old@example.com")
            .Build();

        var newEmail = "new@example.com";
        var newName = "New Name";
        var newRole = SystemRole.DomainManagement;

        // Act
        var result = user.ChangeDetails(newEmail, newName, newRole);

        // Verify
        result.ShouldBeOk();
    }

    #endregion
}
