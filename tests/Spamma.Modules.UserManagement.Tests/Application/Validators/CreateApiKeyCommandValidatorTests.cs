using FluentAssertions;
using Spamma.Modules.UserManagement.Application.Validators.ApiKey;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

namespace Spamma.Modules.UserManagement.Tests.Application.Validators;

public class CreateApiKeyCommandValidatorTests
{
    private readonly CreateApiKeyCommandValidator _validator;

    public CreateApiKeyCommandValidatorTests()
    {
        this._validator = new CreateApiKeyCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_Succeeds()
    {
        // Arrange
        var command = new CreateApiKeyCommand("My API Key", DateTime.UtcNow.AddDays(30));

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyName_Fails()
    {
        // Arrange
        var command = new CreateApiKeyCommand(string.Empty, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithNullName_Fails()
    {
        // Arrange
        var command = new CreateApiKeyCommand(null!, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameExceeding100Characters_Fails()
    {
        // Arrange
        var longName = new string('a', 101);
        var command = new CreateApiKeyCommand(longName, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("100"));
    }

    [Fact]
    public async Task Validate_WithMaximumLengthName_Succeeds()
    {
        // Arrange
        var name = new string('a', 100);
        var command = new CreateApiKeyCommand(name, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await this._validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithDifferentValidNames_Succeeds()
    {
        // Arrange & Act & Assert
        var validNames = new[] { "API Key", "prod_key_v1", "Test Key 123", "K" };
        var expiresAt = DateTime.UtcNow.AddDays(30);
        foreach (var name in validNames)
        {
            var command = new CreateApiKeyCommand(name, expiresAt);
            var result = await this._validator.ValidateAsync(command);
            result.IsValid.Should().BeTrue($"'{name}' should be valid");
        }
    }
}
