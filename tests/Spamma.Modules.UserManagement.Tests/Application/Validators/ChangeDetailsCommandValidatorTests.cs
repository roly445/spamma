using FluentValidation.TestHelper;
using MaybeMonad;
using Moq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Application.Validators;
using Spamma.Modules.UserManagement.Application.Validators.User;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;
using Spamma.Modules.UserManagement.Domain.UserAggregate;

namespace Spamma.Modules.UserManagement.Tests.Application.Validators;

public class ChangeDetailsCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = User.Create(
            userId,
            "John Doe",
            "john@example.com",
            Guid.NewGuid(),
            DateTime.UtcNow,
            SystemRole.UserManagement).Value;

        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(existingUser));

        var validator = new ChangeDetailsCommandValidator(repositoryMock.Object);
        var command = new ChangeDetailsCommand(
            UserId: userId,
            Name: "John Updated",
            EmailAddress: "john@example.com",
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyUserId_HasValidationError()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        // FluentValidation runs ALL validators in chain, so set up email mock
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<User>.Nothing);

        var validator = new ChangeDetailsCommandValidator(repositoryMock.Object);
        var command = new ChangeDetailsCommand(
            UserId: Guid.Empty,
            Name: "John Doe",
            EmailAddress: "john@example.com",
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyEmailAddress_HasValidationError()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        // FluentValidation runs ALL validators in chain even if one fails
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync(string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<User>.Nothing);

        var validator = new ChangeDetailsCommandValidator(repositoryMock.Object);
        var command = new ChangeDetailsCommand(
            UserId: Guid.NewGuid(),
            Name: "John Doe",
            EmailAddress: string.Empty,
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
            .WithErrorMessage("Email Address is required.");
    }

    [Fact]
    public async Task ValidateAsync_InvalidEmailFormat_HasValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = User.Create(
            userId,
            "John Doe",
            "invalid-email",
            Guid.NewGuid(),
            DateTime.UtcNow,
            SystemRole.UserManagement).Value;

        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync("invalid-email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(existingUser));

        var validator = new ChangeDetailsCommandValidator(repositoryMock.Object);
        var command = new ChangeDetailsCommand(
            UserId: userId,
            Name: "John Doe",
            EmailAddress: "invalid-email",
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
            .WithErrorMessage("Email address format is invalid.");
    }

    [Fact]
    public async Task ValidateAsync_EmailAddressAlreadyInUseByAnotherUser_HasValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var otherUser = User.Create(
            otherUserId,
            "Other User",
            "john@example.com",
            Guid.NewGuid(),
            DateTime.UtcNow,
            SystemRole.UserManagement).Value;

        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(otherUser));

        var validator = new ChangeDetailsCommandValidator(repositoryMock.Object);
        var command = new ChangeDetailsCommand(
            UserId: userId,
            Name: "John Doe",
            EmailAddress: "john@example.com",
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
            .WithErrorMessage("Email address is already in use.");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Sonar Code Smell", "S4144:Methods should not have identical implementations", Justification = "Test methods validate different scenarios despite similar structure")]
    public async Task ValidateAsync_SameUserChangingEmailToSameAddress_IsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = User.Create(
            userId,
            "John Doe",
            "john@example.com",
            Guid.NewGuid(),
            DateTime.UtcNow,
            SystemRole.UserManagement).Value;

        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(existingUser));

        var validator = new ChangeDetailsCommandValidator(repositoryMock.Object);
        var command = new ChangeDetailsCommand(
            UserId: userId,
            Name: "John Updated",
            EmailAddress: "john@example.com",
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyName_HasValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = User.Create(
            userId,
            "John Doe",
            "john@example.com",
            Guid.NewGuid(),
            DateTime.UtcNow,
            SystemRole.UserManagement).Value;

        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(existingUser));

        var validator = new ChangeDetailsCommandValidator(repositoryMock.Object);
        var command = new ChangeDetailsCommand(
            UserId: userId,
            Name: string.Empty,
            EmailAddress: "john@example.com",
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required.");
    }

    [Fact]
    public async Task ValidateAsync_NameTooLong_HasValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = User.Create(
            userId,
            "John Doe",
            "john@example.com",
            Guid.NewGuid(),
            DateTime.UtcNow,
            SystemRole.UserManagement).Value;

        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(existingUser));

        var validator = new ChangeDetailsCommandValidator(repositoryMock.Object);
        var command = new ChangeDetailsCommand(
            UserId: userId,
            Name: new string('a', 101),
            EmailAddress: "john@example.com",
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 100 characters.");
    }
}