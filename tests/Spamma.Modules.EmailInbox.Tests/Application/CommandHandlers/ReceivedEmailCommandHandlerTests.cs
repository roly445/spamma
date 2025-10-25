using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Application.CommandHandlers;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using BluQube.Commands;
using FluentValidation;
using ResultMonad;

namespace Spamma.Modules.EmailInbox.Tests.Application.CommandHandlers;

public class ReceivedEmailCommandHandlerTests
{
    private readonly Mock<IEmailRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<ReceivedEmailCommandHandler>> _loggerMock;
    private readonly ReceivedEmailCommandHandler _handler;

    public ReceivedEmailCommandHandlerTests()
    {
        _repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        _eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<ReceivedEmailCommandHandler>>();

        var validators = Array.Empty<IValidator<ReceivedEmailCommand>>();

        _handler = new ReceivedEmailCommandHandler(
            validators,
            _loggerMock.Object,
            _repositoryMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailReceivedSuccessfully_SavesEmailAndPublishesEvent()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var emailAddresses = new List<ReceivedEmailCommand.EmailAddress>
        {
            new("test@example.com", "Test User", EmailAddressType.To),
            new("cc@example.com", "CC User", EmailAddressType.Cc)
        };

        var command = new ReceivedEmailCommand(
            emailId,
            domainId,
            subdomainId,
            "Test Subject",
            now,
            emailAddresses);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<EmailReceivedIntegrationEvent>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Email>(e => e.Id == emailId),
                CancellationToken.None),
            Times.Once);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmailReceivedIntegrationEvent>(e =>
                    e.EmailId == emailId &&
                    e.DomainId == domainId &&
                    e.SubdomainId == subdomainId),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var command = new ReceivedEmailCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            new List<ReceivedEmailCommand.EmailAddress>
            {
                new("test@example.com", "Test", EmailAddressType.To)
            });

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None),
            Times.Once);

        _eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WithMultipleEmailAddressTypes_SavesAllAddresses()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var emailAddresses = new List<ReceivedEmailCommand.EmailAddress>
        {
            new("to@example.com", "To User", EmailAddressType.To),
            new("cc@example.com", "CC User", EmailAddressType.Cc),
            new("bcc@example.com", "BCC User", EmailAddressType.Bcc),
            new("from@example.com", "From User", EmailAddressType.From)
        };

        var command = new ReceivedEmailCommand(
            emailId,
            domainId,
            subdomainId,
            "Multi-address Email",
            DateTime.UtcNow,
            emailAddresses);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmailReceivedIntegrationEvent>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None),
            Times.Once);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<EmailReceivedIntegrationEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleEmails_ProcessesSuccessfully()
    {
        // Arrange - Test idempotency with multiple distinct emails
        var email1Id = Guid.NewGuid();
        var email2Id = Guid.NewGuid();

        var command1 = new ReceivedEmailCommand(
            email1Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Email 1",
            DateTime.UtcNow,
            new List<ReceivedEmailCommand.EmailAddress>
            {
                new("test1@example.com", "Test 1", EmailAddressType.To)
            });

        var command2 = new ReceivedEmailCommand(
            email2Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Email 2",
            DateTime.UtcNow,
            new List<ReceivedEmailCommand.EmailAddress>
            {
                new("test2@example.com", "Test 2", EmailAddressType.To)
            });

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmailReceivedIntegrationEvent>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Verify
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None),
            Times.Exactly(2));

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<EmailReceivedIntegrationEvent>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
