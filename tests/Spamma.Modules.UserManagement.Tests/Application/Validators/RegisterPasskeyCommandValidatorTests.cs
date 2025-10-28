using FluentValidation.TestHelper;
using Moq;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Application.Validators;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate;
using Xunit;
using MaybeMonad;

namespace Spamma.Modules.UserManagement.Tests.Application.Validators;

/// <summary>
/// Tests for RegisterPasskeyCommandValidator to ensure credential uniqueness validation.
/// </summary>
public class RegisterPasskeyCommandValidatorTests
{
    private static RegisterPasskeyCommand CreateValidCommand()
    {
        return new RegisterPasskeyCommand(
            CredentialId: new byte[] { 1, 2, 3, 4, 5 },
            PublicKey: new byte[] { 6, 7, 8, 9, 10 },
            SignCount: 0,
            DisplayName: "My Security Key",
            Algorithm: "ES256");
    }

    [Fact]
    public async Task ValidateAsync_ValidCommand_IsValid()
    {
        // Arrange
        var repositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByCredentialIdAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Passkey>.Nothing);

        var validator = new RegisterPasskeyCommandValidator(repositoryMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EmptyCredentialId_HasValidationError()
    {
        // Arrange
        var repositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Strict);
        var validator = new RegisterPasskeyCommandValidator(repositoryMock.Object);

        var command = CreateValidCommand() with { CredentialId = Array.Empty<byte>() };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CredentialId)
            .WithErrorMessage("Credential ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_DuplicateCredentialId_HasValidationError()
    {
        // Arrange
        var registerResult = Passkey.Register(
            Guid.NewGuid(),
            new byte[] { 1, 2, 3, 4, 5 },
            new byte[] { 6, 7, 8, 9, 10 },
            0,
            "Test Key",
            "ES256",
            DateTime.UtcNow);
        var existingPasskey = registerResult.Value;

        var repositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByCredentialIdAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(existingPasskey));

        var validator = new RegisterPasskeyCommandValidator(repositoryMock.Object);
        var command = CreateValidCommand();

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CredentialId)
            .WithErrorMessage("This credential is already registered by another user.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyDisplayName_HasValidationError()
    {
        // Arrange
        var repositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByCredentialIdAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Passkey>.Nothing);

        var validator = new RegisterPasskeyCommandValidator(repositoryMock.Object);

        var command = CreateValidCommand() with { DisplayName = string.Empty };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name is required.");
    }

    [Fact]
    public async Task ValidateAsync_DisplayNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        var repositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByCredentialIdAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Passkey>.Nothing);

        var validator = new RegisterPasskeyCommandValidator(repositoryMock.Object);

        var longDisplayName = new string('a', 101);
        var command = CreateValidCommand() with { DisplayName = longDisplayName };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name must not exceed 100 characters.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyAlgorithm_HasValidationError()
    {
        // Arrange
        var repositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByCredentialIdAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Passkey>.Nothing);

        var validator = new RegisterPasskeyCommandValidator(repositoryMock.Object);

        var command = CreateValidCommand() with { Algorithm = string.Empty };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Algorithm)
            .WithErrorMessage("Algorithm is required.");
    }

    [Fact]
    public async Task ValidateAsync_EmptyPublicKey_HasValidationError()
    {
        // Arrange
        var repositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByCredentialIdAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Passkey>.Nothing);

        var validator = new RegisterPasskeyCommandValidator(repositoryMock.Object);

        var command = CreateValidCommand() with { PublicKey = Array.Empty<byte>() };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PublicKey)
            .WithErrorMessage("Public key is required.");
    }

    [Fact]
    public async Task ValidateAsync_MultipleValidationErrors_AllReportedTogether()
    {
        // Arrange
        var repositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Strict);
        repositoryMock
            .Setup(x => x.GetByCredentialIdAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Passkey>.Nothing);

        var validator = new RegisterPasskeyCommandValidator(repositoryMock.Object);

        var command = new RegisterPasskeyCommand(
            CredentialId: Array.Empty<byte>(),
            PublicKey: Array.Empty<byte>(),
            SignCount: 0,
            DisplayName: string.Empty,
            Algorithm: string.Empty);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CredentialId);
        result.ShouldHaveValidationErrorFor(x => x.PublicKey);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
        result.ShouldHaveValidationErrorFor(x => x.Algorithm);
    }
}
