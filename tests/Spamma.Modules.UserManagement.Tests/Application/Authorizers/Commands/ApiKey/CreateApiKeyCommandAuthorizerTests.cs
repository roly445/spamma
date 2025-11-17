using FluentAssertions;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.ApiKey;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.ApiKey;

public class CreateApiKeyCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAllowPublicApiAccessRequirement()
    {
        // Arrange
        var authorizer = new CreateApiKeyCommandAuthorizer();
        var command = new CreateApiKeyCommand(Name: "My API Key", ExpiresAt: DateTime.UtcNow.AddDays(30));

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is AllowPublicApiAccessRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsExactlyOneRequirement()
    {
        // Arrange
        var authorizer = new CreateApiKeyCommandAuthorizer();
        var command = new CreateApiKeyCommand(Name: "Test Key", ExpiresAt: DateTime.UtcNow.AddDays(30));

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(1);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentKeyNames()
    {
        // Arrange
        var authorizer1 = new CreateApiKeyCommandAuthorizer();
        var authorizer2 = new CreateApiKeyCommandAuthorizer();
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var command1 = new CreateApiKeyCommand(Name: "Production Key", ExpiresAt: expiresAt);
        var command2 = new CreateApiKeyCommand(Name: "Development Key", ExpiresAt: expiresAt);

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(1);
        authorizer2.Requirements.Should().HaveCount(1);
    }
}
