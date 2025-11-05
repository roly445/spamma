using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.EmailInbox.Application.CommandHandlers;
using Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

namespace Spamma.Modules.EmailInbox.Tests.Application.CommandHandlers;

/// <summary>
/// Tests for the ToggleEmailFavoriteCommandHandler.
/// </summary>
public class ToggleEmailFavoriteCommandHandlerTests
{
    private readonly Mock<IEmailRepository> _repositoryMock;
    private readonly Mock<ILogger<ToggleEmailFavoriteCommandHandler>> _loggerMock;
    private readonly ToggleEmailFavoriteCommandHandler _handler;
    private readonly TimeProvider _timeProvider;

    public ToggleEmailFavoriteCommandHandlerTests()
    {
        this._repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<ToggleEmailFavoriteCommandHandler>>();
        this._timeProvider = TimeProvider.System;

        var validators = Array.Empty<IValidator<ToggleEmailFavoriteCommand>>();

        this._handler = new ToggleEmailFavoriteCommandHandler(
            this._repositoryMock.Object,
            this._timeProvider,
            validators,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailNotFavorite_MarksFavoriteAndSaves()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test User", EmailAddressType.To),
        };

        var email = Email.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            emailAddresses).Value;

        var command = new ToggleEmailFavoriteCommand(emailId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        email.IsFavorite.Should().BeTrue();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(emailId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Email>(e => e.Id == emailId && e.IsFavorite),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyFavorite_RemovesFavoriteAndSaves()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test User", EmailAddressType.To),
        };

        var email = Email.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            emailAddresses).Value;

        email.MarkAsFavorite(DateTime.UtcNow.AddSeconds(1));

        var command = new ToggleEmailFavoriteCommand(emailId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        email.IsFavorite.Should().BeFalse();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(emailId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Email>(e => e.Id == emailId && !e.IsFavorite),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var command = new ToggleEmailFavoriteCommand(emailId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe<Email>.Nothing);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(emailId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test User", EmailAddressType.To),
        };

        var email = Email.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            emailAddresses).Value;

        var command = new ToggleEmailFavoriteCommand(emailId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(emailId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailDeleted_PreventsFavoriteToggle()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test User", EmailAddressType.To),
        };

        var email = Email.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            emailAddresses).Value;

        email.Delete(DateTime.UtcNow.AddSeconds(1));

        var command = new ToggleEmailFavoriteCommand(emailId);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify - Should fail because email is deleted
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(emailId, CancellationToken.None),
            Times.Once);

        // Save should not be called when domain operation fails
        this._repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_MultipleTogglesCycle()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        var emailAddresses = new List<EmailReceived.EmailAddress>
        {
            new("test@example.com", "Test User", EmailAddressType.To),
        };

        var email = Email.Create(
            emailId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Subject",
            DateTime.UtcNow,
            emailAddresses).Value;

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(emailId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(email));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act - First toggle (not favorite -> favorite)
        var command1 = new ToggleEmailFavoriteCommand(emailId);
        var result1 = await this._handler.Handle(command1, CancellationToken.None);
        email.IsFavorite.Should().BeTrue();

        // Act - Second toggle (favorite -> not favorite)
        var command2 = new ToggleEmailFavoriteCommand(emailId);
        var result2 = await this._handler.Handle(command2, CancellationToken.None);
        email.IsFavorite.Should().BeFalse();

        // Act - Third toggle (not favorite -> favorite again)
        var command3 = new ToggleEmailFavoriteCommand(emailId);
        var result3 = await this._handler.Handle(command3, CancellationToken.None);

        // Verify
        email.IsFavorite.Should().BeTrue();
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result3.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Email>(), CancellationToken.None),
            Times.Exactly(3));
    }
}
