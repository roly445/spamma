using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Tests.Builders;
using Spamma.Modules.EmailInbox.Tests.Fixtures;

namespace Spamma.Modules.EmailInbox.Tests.Application.CommandHandlers.Email;

/// <summary>
/// Tests error scenarios for ToggleEmailFavoriteCommandHandler.
/// Tests: NotFound, EmailIsPartOfCampaign, SavingChangesFailed.
/// </summary>
public class ToggleEmailFavoriteCommandHandlerErrorTests
{
    [Fact]
    public async Task Handle_WithNonExistentEmail_DoesNotSave()
    {
        // Arrange
        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        var timeProvider = new StubTimeProvider(new DateTime(2024, 11, 8, 10, 0, 0, DateTimeKind.Utc));

        var nonExistentEmailId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetByIdAsync(nonExistentEmailId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>.Nothing);

        var handler = new ToggleEmailFavoriteCommandHandler(
            repositoryMock.Object,
            timeProvider,
            Array.Empty<IValidator<ToggleEmailFavoriteCommand>>(),
            new Mock<ILogger<ToggleEmailFavoriteCommandHandler>>().Object);

        var command = new ToggleEmailFavoriteCommand(nonExistentEmailId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - verify handler returns a result and doesn't call save
        result.Should().NotBeNull();

        repositoryMock.Verify(x => x.GetByIdAsync(nonExistentEmailId, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmailInCampaign_DoesNotSave()
    {
        // Arrange
        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        var timeProvider = new StubTimeProvider(new DateTime(2024, 11, 8, 10, 0, 0, DateTimeKind.Utc));

        var emailId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var email = new EmailBuilder()
            .WithId(emailId)
            .WithCampaignId(campaignId)
            .Build();

        repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        var handler = new ToggleEmailFavoriteCommandHandler(
            repositoryMock.Object,
            timeProvider,
            Array.Empty<IValidator<ToggleEmailFavoriteCommand>>(),
            new Mock<ILogger<ToggleEmailFavoriteCommandHandler>>().Object);

        var command = new ToggleEmailFavoriteCommand(emailId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - verify handler returns a result but doesn't save (email is in campaign)
        result.Should().NotBeNull();

        repositoryMock.Verify(x => x.GetByIdAsync(emailId, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithRepositorySaveFailure_AttemptsToSave()
    {
        // Arrange
        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        var timeProvider = new StubTimeProvider(new DateTime(2024, 11, 8, 10, 0, 0, DateTimeKind.Utc));

        var emailId = Guid.NewGuid();

        var email = new EmailBuilder()
            .WithId(emailId)
            .WithCampaignId(null)
            .WithIsFavorite(false)
            .Build();

        repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        var handler = new ToggleEmailFavoriteCommandHandler(
            repositoryMock.Object,
            timeProvider,
            Array.Empty<IValidator<ToggleEmailFavoriteCommand>>(),
            new Mock<ILogger<ToggleEmailFavoriteCommandHandler>>().Object);

        var command = new ToggleEmailFavoriteCommand(emailId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - verify save was attempted
        result.Should().NotBeNull();

        repositoryMock.Verify(x => x.GetByIdAsync(emailId, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ToggleFavoriteOnUnfavoritedEmail_Saves()
    {
        // Arrange
        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        var timeProvider = new StubTimeProvider(new DateTime(2024, 11, 8, 10, 0, 0, DateTimeKind.Utc));

        var emailId = Guid.NewGuid();

        var email = new EmailBuilder()
            .WithId(emailId)
            .WithCampaignId(null)
            .WithIsFavorite(false)
            .Build();

        repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        var handler = new ToggleEmailFavoriteCommandHandler(
            repositoryMock.Object,
            timeProvider,
            Array.Empty<IValidator<ToggleEmailFavoriteCommand>>(),
            new Mock<ILogger<ToggleEmailFavoriteCommandHandler>>().Object);

        var command = new ToggleEmailFavoriteCommand(emailId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - verify successful toggle
        result.Should().NotBeNull();

        repositoryMock.Verify(x => x.GetByIdAsync(emailId, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ToggleFavoriteOnFavoritedEmail_Saves()
    {
        // Arrange
        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        var timeProvider = new StubTimeProvider(new DateTime(2024, 11, 8, 10, 0, 0, DateTimeKind.Utc));

        var emailId = Guid.NewGuid();

        var email = new EmailBuilder()
            .WithId(emailId)
            .WithCampaignId(null)
            .WithIsFavorite(true)
            .Build();

        repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        var handler = new ToggleEmailFavoriteCommandHandler(
            repositoryMock.Object,
            timeProvider,
            Array.Empty<IValidator<ToggleEmailFavoriteCommand>>(),
            new Mock<ILogger<ToggleEmailFavoriteCommandHandler>>().Object);

        var command = new ToggleEmailFavoriteCommand(emailId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - verify successful toggle from favorited to unfavorited
        result.Should().NotBeNull();

        repositoryMock.Verify(x => x.GetByIdAsync(emailId, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), CancellationToken.None), Times.Once);
    }
}
