using BluQube.Commands;
using BluQube.Constants;
using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Tests.Fixtures;
using EmailAggregate = Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email;

namespace Spamma.Modules.EmailInbox.Tests.Application.CommandHandlers.Email;

public class ToggleEmailFavoriteCommandHandlerTests
{
    [Fact]
    public async Task Handle_MarkEmailAsFavorite_Succeeds()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var command = new ToggleEmailFavoriteCommand(emailId);

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

        var timeProvider = new StubTimeProvider(DateTime.UtcNow);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<ToggleEmailFavoriteCommandHandler>>();

        var handler = new ToggleEmailFavoriteCommandHandler(
            repositoryMock.Object,
            timeProvider,
            Array.Empty<IValidator<ToggleEmailFavoriteCommand>>(),
            loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResultStatus.Succeeded);
        repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()),
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
        var command = new ToggleEmailFavoriteCommand(emailId);

        var email = EmailAggregate.Create(
            emailId,
            domainId,
            subdomainId,
            "Test Subject",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>()).Value;

        // Capture campaign to set CampaignId
        email.CaptureCampaign(campaignId, DateTime.UtcNow);

        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(email));

        var timeProvider = new StubTimeProvider(DateTime.UtcNow);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<ToggleEmailFavoriteCommandHandler>>();

        var handler = new ToggleEmailFavoriteCommandHandler(
            repositoryMock.Object,
            timeProvider,
            Array.Empty<IValidator<ToggleEmailFavoriteCommand>>(),
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
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var command = new ToggleEmailFavoriteCommand(emailId);

        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<EmailAggregate>.Nothing);

        var timeProvider = new StubTimeProvider(DateTime.UtcNow);
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<ToggleEmailFavoriteCommandHandler>>();

        var handler = new ToggleEmailFavoriteCommandHandler(
            repositoryMock.Object,
            timeProvider,
            Array.Empty<IValidator<ToggleEmailFavoriteCommand>>(),
            loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CommandResultStatus.Failed);
        result.ErrorData!.Code.Should().Be(CommonErrorCodes.NotFound);
    }
}