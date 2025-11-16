using FluentValidation.TestHelper;
using MaybeMonad;
using Moq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Application.Validators;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Domain.UserAggregate;

namespace Spamma.Modules.UserManagement.Tests.Application.Validators;

public class CreateUserCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<User>.Nothing);

        var validator = new CreateUserCommandValidator(repositoryMock.Object);
        var command = new CreateUserCommand(
            UserId: Guid.NewGuid(),
            Name: "John Doe",
            EmailAddress: "john@example.com",
            SendWelcome: true,
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
        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<User>.Nothing);

        var validator = new CreateUserCommandValidator(repositoryMock.Object);
        var command = new CreateUserCommand(
            UserId: Guid.NewGuid(),
            Name: string.Empty,
            EmailAddress: "john@example.com",
            SendWelcome: true,
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
        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<User>.Nothing);

        var validator = new CreateUserCommandValidator(repositoryMock.Object);
        var command = new CreateUserCommand(
            UserId: Guid.NewGuid(),
            Name: new string('a', 101),
            EmailAddress: "john@example.com",
            SendWelcome: true,
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 100 characters.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyEmailAddress_HasValidationError()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        // FluentValidation runs ALL validators in chain even if one fails
        // So MustAsync will be called even though NotEmpty should fail first
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync(string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<User>.Nothing);

        var validator = new CreateUserCommandValidator(repositoryMock.Object);
        var command = new CreateUserCommand(
            UserId: Guid.NewGuid(),
            Name: "John Doe",
            EmailAddress: string.Empty,
            SendWelcome: true,
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
            .WithErrorMessage("Email address is required.");
    }

    [Fact]
    public async Task ValidateAsync_InvalidEmailFormat_HasValidationError()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<User>.Nothing);

        var validator = new CreateUserCommandValidator(repositoryMock.Object);
        var command = new CreateUserCommand(
            UserId: Guid.NewGuid(),
            Name: "John Doe",
            EmailAddress: "not-an-email",
            SendWelcome: true,
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
            .WithErrorMessage("Invalid email address format.");
    }

    [Fact]
    public async Task ValidateAsync_EmailAddressAlreadyInUse_HasValidationError()
    {
        // Arrange
        var existingUser = User.Create(
            Guid.NewGuid(),
            "Existing User",
            "john@example.com",
            Guid.NewGuid(),
            DateTime.UtcNow,
            SystemRole.UserManagement).Value;

        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(existingUser));

        var validator = new CreateUserCommandValidator(repositoryMock.Object);
        var command = new CreateUserCommand(
            UserId: Guid.NewGuid(),
            Name: "John Doe",
            EmailAddress: "john@example.com",
            SendWelcome: true,
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
            .WithErrorMessage("Email address is already in use.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyUserId_HasValidationError()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByEmailAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<User>.Nothing);

        var validator = new CreateUserCommandValidator(repositoryMock.Object);
        var command = new CreateUserCommand(
            UserId: Guid.Empty,
            Name: "John Doe",
            EmailAddress: "john@example.com",
            SendWelcome: true,
            SystemRole: SystemRole.UserManagement);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required.");
    }
}