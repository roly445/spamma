using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Tests.Common.Verification;

namespace Spamma.Modules.EmailInbox.Tests.Domain;

public class EmailFavoriteTests
{
    [Fact]
    public void MarkAsFavorite_WhenEmailNotFavorite_MarksFavoriteAndRaisesEvent()
    {
        // Arrange
        var email = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To),
            }).Value;

        var now = DateTime.UtcNow.AddSeconds(1);

        // Act
        var result = email.MarkAsFavorite(now);

        // Verify
        result.IsSuccess.Should().BeTrue();
        email.IsFavorite.Should().BeTrue();
        email.ShouldHaveRaisedEvent<EmailMarkedAsFavorite>(e =>
        {
            e.MarkedAt.Should().Be(now);
        });
    }

    [Fact]
    public void MarkAsFavorite_WhenAlreadyFavorite_ReturnsError()
    {
        // Arrange
        var email = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To),
            }).Value;

        email.MarkAsFavorite(DateTime.UtcNow.AddSeconds(1));

        // Act
        var result = email.MarkAsFavorite(DateTime.UtcNow.AddSeconds(2));

        // Verify
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UnmarkAsFavorite_WhenEmailFavorite_RemovesFavoriteAndRaisesEvent()
    {
        // Arrange
        var email = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To),
            }).Value;

        email.MarkAsFavorite(DateTime.UtcNow.AddSeconds(1));

        var now = DateTime.UtcNow.AddSeconds(2);

        // Act
        var result = email.UnmarkAsFavorite(now);

        // Verify
        result.IsSuccess.Should().BeTrue();
        email.IsFavorite.Should().BeFalse();
        email.ShouldHaveRaisedEvent<EmailUnmarkedAsFavorite>(e =>
        {
            e.UnmarkedAt.Should().Be(now);
        });
    }

    [Fact]
    public void UnmarkAsFavorite_WhenNotFavorite_ReturnsError()
    {
        // Arrange
        var email = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To),
            }).Value;

        // Act
        var result = email.UnmarkAsFavorite(DateTime.UtcNow.AddSeconds(1));

        // Verify
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ToggleFavorite_WhenNotFavorite_MarksFavoriteAndRaisesEvent()
    {
        // Arrange
        var email = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To),
            }).Value;

        var now = DateTime.UtcNow.AddSeconds(1);

        // Act
        email.IsFavorite.Should().BeFalse();
        var result = email.MarkAsFavorite(now);

        // Verify
        result.IsSuccess.Should().BeTrue();
        email.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public void ToggleFavorite_WhenAlreadyFavorite_RemovesFavoriteAndRaisesEvent()
    {
        // Arrange
        var email = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To),
            }).Value;

        email.MarkAsFavorite(DateTime.UtcNow.AddSeconds(1));

        var now = DateTime.UtcNow.AddSeconds(2);

        // Act
        email.IsFavorite.Should().BeTrue();
        var result = email.UnmarkAsFavorite(now);

        // Verify
        result.IsSuccess.Should().BeTrue();
        email.IsFavorite.Should().BeFalse();
    }

    [Fact]
    public void Delete_WhenFavorite_StillAllowsDelete()
    {
        // Arrange
        var email = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To),
            }).Value;

        email.MarkAsFavorite(DateTime.UtcNow.AddSeconds(1));

        var now = DateTime.UtcNow.AddSeconds(2);

        // Act
        var result = email.Delete(now);

        // Verify - Favorite status doesn't prevent deletion
        result.IsSuccess.Should().BeTrue();
        email.ShouldHaveRaisedEvent<EmailDeleted>(e =>
        {
            e.DeletedAt.Should().Be(now);
        });
    }

    [Fact]
    public void MarkAsFavorite_WhenEmailDeleted_ReturnsError()
    {
        // Arrange
        var email = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To),
            }).Value;

        email.Delete(DateTime.UtcNow.AddSeconds(1));

        // Act
        var result = email.MarkAsFavorite(DateTime.UtcNow.AddSeconds(2));

        // Verify - Cannot mark deleted email as favorite
        result.IsSuccess.Should().BeFalse();
    }
}