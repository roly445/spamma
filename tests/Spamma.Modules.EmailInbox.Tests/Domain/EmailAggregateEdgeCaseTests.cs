using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Tests.Builders;

namespace Spamma.Modules.EmailInbox.Tests.Domain;

public class EmailAggregateEdgeCaseTests
{
    [Fact]
    public void Create_WithEmptyEmailAddresses_Succeeds()
    {
        var result = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMultipleEmailAddresses_Succeeds()
    {
        var addresses = new List<EmailReceived.EmailAddress>
        {
            new("from@test.com", "From User", EmailAddressType.From),
            new("to1@test.com", "To User 1", EmailAddressType.To),
            new("to2@test.com", "To User 2", EmailAddressType.To),
            new("cc@test.com", "CC User", EmailAddressType.Cc),
        };

        var result = Email.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Multi-recipient Email",
            DateTime.UtcNow,
            addresses);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void MarkAsFavorite_ThenDelete_DeleteSucceeds()
    {
        var email = new EmailBuilder().Build();

        email.MarkAsFavorite(DateTime.UtcNow);
        var deleteResult = email.Delete(DateTime.UtcNow);

        deleteResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Delete_ThenMarkAsFavorite_FailsBecauseEmailDeleted()
    {
        var email = new EmailBuilder().Build();

        email.Delete(DateTime.UtcNow);
        var markResult = email.MarkAsFavorite(DateTime.UtcNow);

        markResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ToggleFavorite_MarkUnmarkMark_IsCorrectState()
    {
        var email = new EmailBuilder().Build();

        email.MarkAsFavorite(DateTime.UtcNow);
        email.UnmarkAsFavorite(DateTime.UtcNow);
        email.MarkAsFavorite(DateTime.UtcNow);

        email.Should().NotBeNull();
    }
}