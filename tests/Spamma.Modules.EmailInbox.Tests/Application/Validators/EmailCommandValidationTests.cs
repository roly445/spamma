using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Application.CommandHandlers.Email;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Application.Validators.Email;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Tests.Fixtures;

namespace Spamma.Modules.EmailInbox.Tests.Application.Validators;

/// <summary>
/// Tests validation failures for email command validators.
/// Tests: DeleteEmailCommand, ToggleEmailFavoriteCommand with empty GUIDs.
/// </summary>
public class EmailCommandValidationTests
{
    [Fact]
    public async Task DeleteEmailCommand_WithEmptyEmailId_FailsValidation()
    {
        // Arrange
        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        var timeProvider = new StubTimeProvider(new DateTime(2024, 11, 8, 10, 0, 0, DateTimeKind.Utc));
        var eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);

        var validators = new IValidator<DeleteEmailCommand>[]
        {
            new DeleteEmailCommandValidator(),
        };

        var handler = new DeleteEmailCommandHandler(
            repositoryMock.Object,
            timeProvider,
            eventPublisherMock.Object,
            validators,
            new Mock<ILogger<DeleteEmailCommandHandler>>().Object);

        var command = new DeleteEmailCommand(Guid.Empty);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - validation should fail, repository should never be called
        result.Should().NotBeNull();

        repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), It.IsAny<CancellationToken>()), Times.Never);
        eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<EmailDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleEmailFavoriteCommand_WithEmptyEmailId_FailsValidation()
    {
        // Arrange
        var repositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        var timeProvider = new StubTimeProvider(new DateTime(2024, 11, 8, 10, 0, 0, DateTimeKind.Utc));

        var validators = new IValidator<ToggleEmailFavoriteCommand>[]
        {
            new ToggleEmailFavoriteCommandValidator(),
        };

        var handler = new ToggleEmailFavoriteCommandHandler(
            repositoryMock.Object,
            timeProvider,
            validators,
            new Mock<ILogger<ToggleEmailFavoriteCommandHandler>>().Object);

        var command = new ToggleEmailFavoriteCommand(Guid.Empty);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - validation should fail, repository should never be called
        result.Should().NotBeNull();

        repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void DeleteEmailCommandValidator_WithEmptyGuid_ReturnsValidationError()
    {
        // Arrange
        var validator = new DeleteEmailCommandValidator();
        var command = new DeleteEmailCommand(Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PropertyName.Should().Be("EmailId");
        result.Errors[0].ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public void ToggleEmailFavoriteCommandValidator_WithEmptyGuid_ReturnsValidationError()
    {
        // Arrange
        var validator = new ToggleEmailFavoriteCommandValidator();
        var command = new ToggleEmailFavoriteCommand(Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PropertyName.Should().Be("EmailId");
        result.Errors[0].ErrorMessage.Should().Contain("required");
    }
}
