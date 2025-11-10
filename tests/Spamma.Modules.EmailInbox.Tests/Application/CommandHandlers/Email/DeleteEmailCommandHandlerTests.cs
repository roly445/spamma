using BluQube.Constants;
using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Moq;
using ResultMonad;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Tests.Fixtures;
using EmailAggregate = Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email;

namespace Spamma.Modules.EmailInbox.Tests.Application.CommandHandlers.Email;

public class DeleteEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidEmail_DeletesSuccessfully()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var command = new DeleteEmailCommand(emailId);

        var email = EmailAggregate.Create(
            emailId,
            domainId,
            subdomainId,
            "Test Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>()).Value;

        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(email));

        repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmailDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var timeProvider = new StubTimeProvider(DateTime.UtcNow);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<DeleteEmailCommandHandler>>();

        var handler = new DeleteEmailCommandHandler(
            repositoryMock.Object,
            timeProvider,
            eventPublisherMock.Object,
            Array.Empty<IValidator<DeleteEmailCommand>>(),
            loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResultStatus.Succeeded);
        repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()),
            Times.Once);
        eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<EmailDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCampaignBoundEmail_ReturnsFailed()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var command = new DeleteEmailCommand(emailId);

        // Create email with campaign association
        var email = EmailAggregate.Create(
            emailId,
            domainId,
            subdomainId,
            "Test Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>(),
            campaignId).Value;

        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(email));

        var eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);

        var timeProvider = new StubTimeProvider(DateTime.UtcNow);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<DeleteEmailCommandHandler>>();

        var handler = new DeleteEmailCommandHandler(
            repositoryMock.Object,
            timeProvider,
            eventPublisherMock.Object,
            Array.Empty<IValidator<DeleteEmailCommand>>(),
            loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResultStatus.Failed);
        result.ErrorData.Should().NotBeNull();
        result.ErrorData!.Code.Should().Be(EmailInboxErrorCodes.EmailIsPartOfCampaign);
        repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
        eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<EmailDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var command = new DeleteEmailCommand(emailId);

        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<EmailAggregate>.Nothing);

        var eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);

        var timeProvider = new StubTimeProvider(DateTime.UtcNow);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<DeleteEmailCommandHandler>>();

        var handler = new DeleteEmailCommandHandler(
            repositoryMock.Object,
            timeProvider,
            eventPublisherMock.Object,
            Array.Empty<IValidator<DeleteEmailCommand>>(),
            loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResultStatus.Failed);
        result.ErrorData!.Code.Should().Be(CommonErrorCodes.NotFound);
    }
}
