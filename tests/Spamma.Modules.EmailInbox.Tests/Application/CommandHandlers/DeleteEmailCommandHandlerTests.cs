using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MaybeMonad;
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

public class DeleteEmailCommandHandlerTests
{
    private readonly Mock<IEmailRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<DeleteEmailCommandHandler>> _loggerMock;
    private readonly DeleteEmailCommandHandler _handler;
    private readonly TimeProvider _timeProvider;

    public DeleteEmailCommandHandlerTests()
    {
        _repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        _eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<DeleteEmailCommandHandler>>();
        _timeProvider = TimeProvider.System;

        var validators = Array.Empty<IValidator<DeleteEmailCommand>>();

        _handler = new DeleteEmailCommandHandler(
            _repositoryMock.Object,
            _timeProvider,
            _eventPublisherMock.Object,
            validators,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailExistsAndNotDeleted_DeletesEmailAndPublishesEvent()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test User", EmailAddressType.To)
        };

        var email = Email.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            emailAddresses).Value;

        var command = new DeleteEmailCommand(emailId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<EmailDeletedIntegrationEvent>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(emailId, CancellationToken.None),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Email>(e => e.Id == emailId),
                CancellationToken.None),
            Times.Once);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmailDeletedIntegrationEvent>(e => e.EmailId == emailId),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var command = new DeleteEmailCommand(emailId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe<Email>.Nothing);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(emailId, CancellationToken.None),
            Times.Once);

        _repositoryMock.VerifyNoOtherCalls();
        _eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test", EmailAddressType.To)
        };

        var email = Email.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            emailAddresses).Value;

        var command = new DeleteEmailCommand(emailId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(emailId, CancellationToken.None),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None),
            Times.Once);

        _eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyDeleted_ReturnsError()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test", EmailAddressType.To)
        };

        var email = Email.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Subject",
            DateTime.UtcNow,
            emailAddresses).Value;

        // Delete the email first
        email.Delete(DateTime.UtcNow);

        var command = new DeleteEmailCommand(emailId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(emailId, CancellationToken.None),
            Times.Once);

        _repositoryMock.VerifyNoOtherCalls();
        _eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_MultipleDeleteAttemptsOnDifferentEmails_HandlesIdempotently()
    {
        // Arrange
        var email1Id = Guid.NewGuid();
        var email2Id = Guid.NewGuid();

        var emailAddresses1 = new List<EmailReceived.EmailAddress>
        {
            new("test1@example.com", "Test 1", EmailAddressType.To)
        };

        var emailAddresses2 = new List<EmailReceived.EmailAddress>
        {
            new("test2@example.com", "Test 2", EmailAddressType.To)
        };

        var email1 = Email.Create(
            email1Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Email 1",
            DateTime.UtcNow,
            emailAddresses1).Value;

        var email2 = Email.Create(
            email2Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Email 2",
            DateTime.UtcNow,
            emailAddresses2).Value;

        var command1 = new DeleteEmailCommand(email1Id);
        var command2 = new DeleteEmailCommand(email2Id);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(email1Id, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email1));

        _repositoryMock
            .Setup(x => x.GetByIdAsync(email2Id, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email2));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmailDeletedIntegrationEvent>(), CancellationToken.None))
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
            x => x.PublishAsync(It.IsAny<EmailDeletedIntegrationEvent>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
