using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Tests.Builders;
using Spamma.Modules.EmailInbox.Tests.Fixtures;

namespace Spamma.Modules.EmailInbox.Tests.Application.CommandHandlers.Email;

public class DeleteEmailCommandHandlerErrorTests
{
    [Fact]
    public async Task Handle_WithNonExistentEmail_DoesNotSaveOrPublishEvent()
    {
        // Arrange
        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        var eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        var timeProvider = new StubTimeProvider(new DateTime(2024, 11, 8, 10, 0, 0, DateTimeKind.Utc));

        var nonExistentEmailId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetByIdAsync(nonExistentEmailId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>.Nothing);

        var handler = new DeleteEmailCommandHandler(
            repositoryMock.Object,
            timeProvider,
            eventPublisherMock.Object,
            Array.Empty<IValidator<DeleteEmailCommand>>(),
            new Mock<ILogger<DeleteEmailCommandHandler>>().Object);

        var command = new DeleteEmailCommand(nonExistentEmailId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - verify handler returns a result and doesn't call save or publish
        result.Should().NotBeNull();

        repositoryMock.Verify(x => x.GetByIdAsync(nonExistentEmailId, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), It.IsAny<CancellationToken>()), Times.Never);
        eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<EmailDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmailInCampaign_DoesNotSaveOrPublishEvent()
    {
        // Arrange
        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        var eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
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

        var handler = new DeleteEmailCommandHandler(
            repositoryMock.Object,
            timeProvider,
            eventPublisherMock.Object,
            Array.Empty<IValidator<DeleteEmailCommand>>(),
            new Mock<ILogger<DeleteEmailCommandHandler>>().Object);

        var command = new DeleteEmailCommand(emailId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - verify handler returns a result but doesn't save or publish (email is in campaign)
        result.Should().NotBeNull();

        repositoryMock.Verify(x => x.GetByIdAsync(emailId, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), It.IsAny<CancellationToken>()), Times.Never);
        eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<EmailDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithRepositorySaveFailure_DoesNotPublishEvent()
    {
        // Arrange
        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        var eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        var timeProvider = new StubTimeProvider(new DateTime(2024, 11, 8, 10, 0, 0, DateTimeKind.Utc));

        var emailId = Guid.NewGuid();

        var email = new EmailBuilder()
            .WithId(emailId)
            .WithCampaignId(null)
            .Build();

        repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        var handler = new DeleteEmailCommandHandler(
            repositoryMock.Object,
            timeProvider,
            eventPublisherMock.Object,
            Array.Empty<IValidator<DeleteEmailCommand>>(),
            new Mock<ILogger<DeleteEmailCommandHandler>>().Object);

        var command = new DeleteEmailCommand(emailId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - verify save was attempted but event was not published due to save failure
        result.Should().NotBeNull();

        repositoryMock.Verify(x => x.GetByIdAsync(emailId, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), CancellationToken.None), Times.Once);
        eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<EmailDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}