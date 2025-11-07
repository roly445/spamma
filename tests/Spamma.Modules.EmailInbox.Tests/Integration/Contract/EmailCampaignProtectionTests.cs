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
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Tests.Fixtures;
using EmailAggregate = Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email;

namespace Spamma.Modules.EmailInbox.Tests.Integration.Contract;

/// <summary>
/// Contract tests verifying API behavior for campaign protection rules.
/// These tests verify the contract between the client (API endpoint) and the handlers:
/// - DeleteEmail and ToggleEmailFavorite commands must reject campaign-bound emails.
/// - Error responses must use the EmailIsPartOfCampaign error code.
/// - Status must indicate failure (CommandResultStatus.Failed).
/// </summary>
public class EmailCampaignProtectionTests
{
    private readonly Mock<IEmailRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<DeleteEmailCommandHandler>> _deleteLoggerMock;
    private readonly Mock<ILogger<ToggleEmailFavoriteCommandHandler>> _toggleLoggerMock;
    private readonly DeleteEmailCommandHandler _deleteHandler;
    private readonly ToggleEmailFavoriteCommandHandler _toggleHandler;
    private readonly TimeProvider _timeProvider;

    public EmailCampaignProtectionTests()
    {
        this._repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        this._eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        this._deleteLoggerMock = new Mock<ILogger<DeleteEmailCommandHandler>>();
        this._toggleLoggerMock = new Mock<ILogger<ToggleEmailFavoriteCommandHandler>>();
        this._timeProvider = new StubTimeProvider(DateTime.UtcNow);

        var validators = Array.Empty<IValidator<DeleteEmailCommand>>();
        var toggleValidators = Array.Empty<IValidator<ToggleEmailFavoriteCommand>>();

        this._deleteHandler = new DeleteEmailCommandHandler(
            this._repositoryMock.Object,
            this._timeProvider,
            this._eventPublisherMock.Object,
            validators,
            this._deleteLoggerMock.Object);

        this._toggleHandler = new ToggleEmailFavoriteCommandHandler(
            this._repositoryMock.Object,
            this._timeProvider,
            toggleValidators,
            this._toggleLoggerMock.Object);
    }

    /// <summary>
    /// Contract: DeleteEmail API must reject campaign-bound emails with EmailIsPartOfCampaign error code.
    /// Endpoint: email-inbox/delete-email
    /// Request: DeleteEmailCommand(EmailId)
    /// Response: CommandResult with Status=Failed, ErrorData.Code=EmailIsPartOfCampaign.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteEmailContract_CampaignBoundEmail_RejectsWithEmailIsPartOfCampaignError()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var command = new DeleteEmailCommand(emailId);

        // Create email with campaign binding
        var email = EmailAggregate.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Campaign Email",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test User", EmailAddressType.To),
            }).Value;

        // Capture campaign
        email.CaptureCampaign(campaignId, DateTime.UtcNow);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(email));

        // Act
        var result = await this._deleteHandler.Handle(command, CancellationToken.None);

        // Assert: Contract verification
        result.Should().NotBeNull("DeleteEmail must return a result");
        result.Status.Should().Be(CommandResultStatus.Failed, "DeleteEmail must fail for campaign-bound emails");
        result.ErrorData.Should().NotBeNull("Error data must be provided");
        result.ErrorData!.Code.Should().Be(EmailInboxErrorCodes.EmailIsPartOfCampaign, "Error code must be EmailIsPartOfCampaign");
        result.ErrorData.Message.Should().ContainAny("campaign", "Campaign");

        // Verify repository was NOT updated (no save operation)
        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Email repository should not be updated for campaign-bound emails");

        // Verify no integration event published
        this._eventPublisherMock.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Contract: ToggleEmailFavorite API must reject campaign-bound emails with EmailIsPartOfCampaign error code.
    /// Endpoint: email-inbox/toggle-favorite
    /// Request: ToggleEmailFavoriteCommand(EmailId)
    /// Response: CommandResult with Status=Failed, ErrorData.Code=EmailIsPartOfCampaign.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleEmailFavoriteContract_CampaignBoundEmail_RejectsWithEmailIsPartOfCampaignError()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var command = new ToggleEmailFavoriteCommand(emailId);

        // Create email with campaign binding
        var email = EmailAggregate.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Campaign Email",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test User", EmailAddressType.To),
            }).Value;

        // Capture campaign
        email.CaptureCampaign(campaignId, DateTime.UtcNow);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(email));

        // Act
        var result = await this._toggleHandler.Handle(command, CancellationToken.None);

        // Assert: Contract verification
        result.Should().NotBeNull("ToggleEmailFavorite must return a result");
        result.Status.Should().Be(CommandResultStatus.Failed, "ToggleEmailFavorite must fail for campaign-bound emails");
        result.ErrorData.Should().NotBeNull("Error data must be provided");
        result.ErrorData!.Code.Should().Be(EmailInboxErrorCodes.EmailIsPartOfCampaign, "Error code must be EmailIsPartOfCampaign");
        result.ErrorData.Message.Should().ContainAny("campaign", "Campaign");

        // Verify repository was NOT updated (no save operation)
        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Email repository should not be updated for campaign-bound emails");

        // Verify no integration event published
        this._eventPublisherMock.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Contract: DeleteEmail API must successfully delete non-campaign emails.
    /// This verifies the positive path to ensure campaign checks don't break normal operations.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteEmailContract_NonCampaignEmail_SucceedsNormally()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var command = new DeleteEmailCommand(emailId);

        // Create email WITHOUT campaign binding
        var email = EmailAggregate.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Normal Email",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test User", EmailAddressType.To),
            }).Value;

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(email));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        this._eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<Common.IntegrationEvents.EmailInbox.EmailDeletedIntegrationEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this._deleteHandler.Handle(command, CancellationToken.None);

        // Assert: Contract verification
        result.Should().NotBeNull("DeleteEmail must return a result");
        result.Status.Should().Be(CommandResultStatus.Succeeded, "DeleteEmail must succeed for non-campaign emails");

        // Verify repository WAS updated
        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Email repository must be updated for successful delete");

        // Verify integration event published
        this._eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<Common.IntegrationEvents.EmailInbox.EmailDeletedIntegrationEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Integration event must be published on successful delete");
    }

    /// <summary>
    /// Contract: ToggleEmailFavorite API must successfully toggle non-campaign emails.
    /// This verifies the positive path to ensure campaign checks don't break normal operations.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleEmailFavoriteContract_NonCampaignEmail_SucceedsNormally()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var command = new ToggleEmailFavoriteCommand(emailId);

        // Create email WITHOUT campaign binding
        var email = EmailAggregate.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Normal Email",
            DateTime.UtcNow,
            new List<EmailReceived.EmailAddress>
            {
                new("test@example.com", "Test User", EmailAddressType.To),
            }).Value;

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(email));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await this._toggleHandler.Handle(command, CancellationToken.None);

        // Assert: Contract verification
        result.Should().NotBeNull("ToggleEmailFavorite must return a result");
        result.Status.Should().Be(CommandResultStatus.Succeeded, "ToggleEmailFavorite must succeed for non-campaign emails");

        // Verify repository WAS updated
        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Email repository must be updated for successful toggle");
    }

    /// <summary>
    /// Contract: Campaign protection must not affect other error cases.
    /// NotFound errors should still be returned normally.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteEmailContract_NonExistentEmail_ReturnsNotFoundError()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var command = new DeleteEmailCommand(emailId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<EmailAggregate>.Nothing);

        // Act
        var result = await this._deleteHandler.Handle(command, CancellationToken.None);

        // Assert: NotFound error should be returned, not campaign error
        result.Should().NotBeNull();
        result.Status.Should().Be(CommandResultStatus.Failed);
        result.ErrorData.Should().NotBeNull();
        result.ErrorData!.Code.Should().Be(CommonErrorCodes.NotFound, "NotFound should be returned before campaign check");
    }

    /// <summary>
    /// Contract: Campaign protection is fail-fast - it executes before any domain state changes.
    /// Multiple campaign checks should all fail immediately.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CampaignProtection_FailsFastBeforeStateChanges()
    {
        // Arrange: Create multiple campaign-bound emails
        var emailId1 = Guid.NewGuid();
        var emailId2 = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var email1 = EmailAggregate.Create(emailId1, Guid.NewGuid(), Guid.NewGuid(), "Email 1", DateTime.UtcNow,
            new List<EmailReceived.EmailAddress> { new("test1@example.com", "Test 1", EmailAddressType.To) }).Value;
        email1.CaptureCampaign(campaignId, DateTime.UtcNow);

        var email2 = EmailAggregate.Create(emailId2, Guid.NewGuid(), Guid.NewGuid(), "Email 2", DateTime.UtcNow,
            new List<EmailReceived.EmailAddress> { new("test2@example.com", "Test 2", EmailAddressType.To) }).Value;
        email2.CaptureCampaign(campaignId, DateTime.UtcNow);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(email1));

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(email2));

        // Act: Attempt to delete both
        var command1 = new DeleteEmailCommand(emailId1);
        var command2 = new DeleteEmailCommand(emailId2);

        var result1 = await this._deleteHandler.Handle(command1, CancellationToken.None);
        var result2 = await this._deleteHandler.Handle(command2, CancellationToken.None);

        // Assert: Both should fail with campaign error
        result1.Status.Should().Be(CommandResultStatus.Failed);
        result1.ErrorData!.Code.Should().Be(EmailInboxErrorCodes.EmailIsPartOfCampaign);

        result2.Status.Should().Be(CommandResultStatus.Failed);
        result2.ErrorData!.Code.Should().Be(EmailInboxErrorCodes.EmailIsPartOfCampaign);

        // Verify no save operations occurred at all
        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<EmailAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "No save operations should occur for campaign-bound emails");
    }
}

