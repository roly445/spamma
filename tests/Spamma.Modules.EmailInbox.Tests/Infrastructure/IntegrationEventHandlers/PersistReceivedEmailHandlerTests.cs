using BluQube.Commands;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Infrastructure.IntegrationEventHandlers;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Tests for PersistReceivedEmailHandler integration event handling.
/// </summary>
public class PersistReceivedEmailHandlerTests
{
    private readonly Mock<ICommander> _commanderMock;
    private readonly Mock<ILogger<PersistReceivedEmailHandler>> _loggerMock;
    private readonly PersistReceivedEmailHandler _handler;

    public PersistReceivedEmailHandlerTests()
    {
        _commanderMock = new Mock<ICommander>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<PersistReceivedEmailHandler>>();
        _handler = new PersistReceivedEmailHandler(_loggerMock.Object, _commanderMock.Object);
    }

    [Fact]
    public async Task OnEmailReceived_WithValidEvent_SendsReceivedEmailCommand()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var receivedAt = DateTime.UtcNow;

        var integrationEvent = new EmailReceivedIntegrationEvent(
            EmailId: emailId,
            DomainId: domainId,
            SubdomainId: subdomainId,
            Subject: "Test Subject",
            ReceivedAt: receivedAt,
            Recipients: new List<EmailReceivedIntegrationEvent.EmailReceivedAddress>
            {
                new("user@example.com", "User Name", 1),
            },
            CampaignValue: null,
            FromAddress: "sender@example.com",
            ToAddress: "user@example.com",
            ChaosAddressId: null);

        _commanderMock
            .Setup(x => x.Send(
                It.Is<ReceivedEmailCommand>(cmd =>
                    cmd.EmailId == emailId &&
                    cmd.DomainId == domainId &&
                    cmd.SubdomainId == subdomainId &&
                    cmd.Subject == "Test Subject" &&
                    cmd.WhenSent == receivedAt &&
                    cmd.EmailAddresses.Count == 1 &&
                    cmd.EmailAddresses[0].Address == "user@example.com" &&
                    cmd.EmailAddresses[0].Name == "User Name"),
                CancellationToken.None))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        await _handler.OnEmailReceived(integrationEvent);

        // Assert
        _commanderMock.Verify(
            x => x.Send(It.IsAny<ReceivedEmailCommand>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task OnEmailReceived_WithChaosAddressId_RecordsChaosAddressReceived()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var chaosAddressId = Guid.NewGuid();

        var integrationEvent = new EmailReceivedIntegrationEvent(
            EmailId: emailId,
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            Subject: "Test",
            ReceivedAt: DateTime.UtcNow,
            Recipients: new List<EmailReceivedIntegrationEvent.EmailReceivedAddress>
            {
                new("test@example.com", string.Empty, 1),
            },
            CampaignValue: null,
            FromAddress: "sender@example.com",
            ToAddress: "test@example.com",
            ChaosAddressId: chaosAddressId);

        _commanderMock
            .Setup(x => x.Send(It.IsAny<ReceivedEmailCommand>(), CancellationToken.None))
            .ReturnsAsync(CommandResult.Succeeded());

        _commanderMock
            .Setup(x => x.Send(
                It.Is<RecordChaosAddressReceivedCommand>(cmd => cmd.ChaosAddressId == chaosAddressId),
                CancellationToken.None))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        await _handler.OnEmailReceived(integrationEvent);

        // Assert
        _commanderMock.Verify(
            x => x.Send(It.Is<RecordChaosAddressReceivedCommand>(cmd => cmd.ChaosAddressId == chaosAddressId), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task OnEmailReceived_WithCampaignValue_RecordsCampaignCapture()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var campaignValue = "test-campaign";

        var integrationEvent = new EmailReceivedIntegrationEvent(
            EmailId: emailId,
            DomainId: domainId,
            SubdomainId: subdomainId,
            Subject: "Test",
            ReceivedAt: DateTime.UtcNow,
            Recipients: new List<EmailReceivedIntegrationEvent.EmailReceivedAddress>
            {
                new("test@example.com", string.Empty, 1),
            },
            CampaignValue: campaignValue,
            FromAddress: "sender@example.com",
            ToAddress: "test@example.com",
            ChaosAddressId: null);

        _commanderMock
            .Setup(x => x.Send(It.IsAny<ReceivedEmailCommand>(), CancellationToken.None))
            .ReturnsAsync(CommandResult.Succeeded());

        _commanderMock
            .Setup(x => x.Send(
                It.Is<RecordCampaignCaptureCommand>(cmd =>
                    cmd.DomainId == domainId &&
                    cmd.SubdomainId == subdomainId &&
                    cmd.MessageId == emailId &&
                    cmd.CampaignValue == campaignValue),
                CancellationToken.None))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        await _handler.OnEmailReceived(integrationEvent);

        // Assert
        _commanderMock.Verify(
            x => x.Send(It.Is<RecordCampaignCaptureCommand>(cmd => cmd.CampaignValue == campaignValue), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task OnEmailReceived_WithMultipleRecipients_ConvertsAllAddressTypes()
    {
        // Arrange
        var recipients = new List<EmailReceivedIntegrationEvent.EmailReceivedAddress>
        {
            new("to@example.com", "To User", 1),
            new("cc@example.com", "CC User", 2),
            new("bcc@example.com", "BCC User", 3),
        };

        var integrationEvent = new EmailReceivedIntegrationEvent(
            EmailId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            Subject: "Test",
            ReceivedAt: DateTime.UtcNow,
            Recipients: recipients,
            CampaignValue: null,
            FromAddress: "sender@example.com",
            ToAddress: "to@example.com",
            ChaosAddressId: null);

        _commanderMock
            .Setup(x => x.Send(
                It.Is<ReceivedEmailCommand>(cmd =>
                    cmd.EmailAddresses.Count == 3 &&
                    cmd.EmailAddresses.Any(r => r.Address == "to@example.com") &&
                    cmd.EmailAddresses.Any(r => r.Address == "cc@example.com") &&
                    cmd.EmailAddresses.Any(r => r.Address == "bcc@example.com")),
                CancellationToken.None))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        await _handler.OnEmailReceived(integrationEvent);

        // Assert
        _commanderMock.Verify(
            x => x.Send(It.IsAny<ReceivedEmailCommand>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task OnEmailReceived_WithNullSubject_ConvertsToEmptyString()
    {
        // Arrange
        var integrationEvent = new EmailReceivedIntegrationEvent(
            EmailId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            Subject: null,
            ReceivedAt: DateTime.UtcNow,
            Recipients: new List<EmailReceivedIntegrationEvent.EmailReceivedAddress>
            {
                new("test@example.com", string.Empty, 1),
            },
            CampaignValue: null,
            FromAddress: "sender@example.com",
            ToAddress: "test@example.com",
            ChaosAddressId: null);

        _commanderMock
            .Setup(x => x.Send(
                It.Is<ReceivedEmailCommand>(cmd => cmd.Subject == string.Empty),
                CancellationToken.None))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        await _handler.OnEmailReceived(integrationEvent);

        // Assert
        _commanderMock.Verify(
            x => x.Send(It.Is<ReceivedEmailCommand>(cmd => cmd.Subject == string.Empty), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task OnEmailReceived_WithNullRecipientDisplayName_ConvertsToEmptyString()
    {
        // Arrange
        var integrationEvent = new EmailReceivedIntegrationEvent(
            EmailId: Guid.NewGuid(),
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            Subject: "Test",
            ReceivedAt: DateTime.UtcNow,
            Recipients: new List<EmailReceivedIntegrationEvent.EmailReceivedAddress>
            {
                new("test@example.com", null, 1),
            },
            CampaignValue: null,
            FromAddress: "sender@example.com",
            ToAddress: "test@example.com",
            ChaosAddressId: null);

        _commanderMock
            .Setup(x => x.Send(
                It.Is<ReceivedEmailCommand>(cmd =>
                    cmd.EmailAddresses.Count == 1 &&
                    cmd.EmailAddresses[0].Name == string.Empty),
                CancellationToken.None))
            .ReturnsAsync(CommandResult.Succeeded());

        // Act
        await _handler.OnEmailReceived(integrationEvent);

        // Assert
        _commanderMock.Verify(
            x => x.Send(It.IsAny<ReceivedEmailCommand>(), CancellationToken.None),
            Times.Once);
    }
}
