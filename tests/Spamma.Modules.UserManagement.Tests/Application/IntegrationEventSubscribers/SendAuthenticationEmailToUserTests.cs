using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using ResultMonad;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Application;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;
using Spamma.Modules.UserManagement.Application.IntegrationEventSubscribers;

namespace Spamma.Modules.UserManagement.Tests.Application.IntegrationEventSubscribers;

public class SendAuthenticationEmailToUserTests
{
    private readonly Mock<IAuthTokenProvider> _authTokenProviderMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly IOptions<Settings> _settings;
    private readonly SendAuthenticationEmailToUser _subscriber;

    public SendAuthenticationEmailToUserTests()
    {
        _authTokenProviderMock = new Mock<IAuthTokenProvider>(MockBehavior.Strict);
        _emailSenderMock = new Mock<IEmailSender>(MockBehavior.Strict);
        _settings = Options.Create(new Settings { BaseUri = "https://spamma.io" });
        _subscriber = new SendAuthenticationEmailToUser(_authTokenProviderMock.Object, _emailSenderMock.Object, _settings);
    }

    [Fact]
    public async Task Process_WhenTokenGenerationSucceeds_SendsAuthenticationEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authAttemptId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();
        var ev = new AuthenticationStartedIntegrationEvent(
            UserId: userId,
            SecurityStamp: securityStamp,
            AuthenticationAttemptId: authAttemptId,
            Name: "John Doe",
            EmailAddress: "john@example.com",
            WhenHappened: DateTime.UtcNow);

        _authTokenProviderMock
            .Setup(x => x.GenerateAuthenticationToken(It.Is<IAuthTokenProvider.AuthenticationTokenModel>(
                m => m.UserId == userId && m.SecurityStamp == securityStamp && m.AuthenticationAttemptId == authAttemptId)))
            .Returns(Result.Ok("generated-token-abc123"));

        _emailSenderMock
            .Setup(x => x.SendEmailAsync(
                It.Is<string>(name => name == "John Doe"),
                It.Is<string>(email => email == "john@example.com"),
                It.Is<string>(subject => subject == "Authenticate your Spamma account"),
                It.IsAny<List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await _subscriber.Process(ev);

        // Assert
        _emailSenderMock.Verify(
            x => x.SendEmailAsync(
                "John Doe",
                "john@example.com",
                "Authenticate your Spamma account",
                It.IsAny<List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Process_WhenTokenGenerationSucceeds_EmailContainsEncodedToken()
    {
        // Arrange
        var ev = new AuthenticationStartedIntegrationEvent(
            UserId: Guid.NewGuid(),
            SecurityStamp: Guid.NewGuid(),
            AuthenticationAttemptId: Guid.NewGuid(),
            Name: "Test User",
            EmailAddress: "test@example.com",
            WhenHappened: DateTime.UtcNow);

        _authTokenProviderMock
            .Setup(x => x.GenerateAuthenticationToken(It.IsAny<IAuthTokenProvider.AuthenticationTokenModel>()))
            .Returns(Result.Ok("test+token/special=chars"));

        List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>? capturedEmailBody = null;

        _emailSenderMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>, CancellationToken>(
                (_, _, _, body, _) => capturedEmailBody = body)
            .ReturnsAsync(Result.Ok());

        // Act
        await _subscriber.Process(ev);

        // Assert
        capturedEmailBody.Should().NotBeNull();
        var allText = string.Join(" ", capturedEmailBody!.SelectMany(t => t.Item2));
        allText.Should().Contain("test%2Btoken%2Fspecial%3Dchars"); // URL encoded
    }

    [Fact]
    public async Task Process_WhenTokenGenerationSucceeds_EmailContainsLoginUri()
    {
        // Arrange
        var ev = new AuthenticationStartedIntegrationEvent(
            UserId: Guid.NewGuid(),
            SecurityStamp: Guid.NewGuid(),
            AuthenticationAttemptId: Guid.NewGuid(),
            Name: "Test User",
            EmailAddress: "test@example.com",
            WhenHappened: DateTime.UtcNow);

        _authTokenProviderMock
            .Setup(x => x.GenerateAuthenticationToken(It.IsAny<IAuthTokenProvider.AuthenticationTokenModel>()))
            .Returns(Result.Ok("token123"));

        List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>? capturedEmailBody = null;

        _emailSenderMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>, CancellationToken>(
                (_, _, _, body, _) => capturedEmailBody = body)
            .ReturnsAsync(Result.Ok());

        // Act
        await _subscriber.Process(ev);

        // Assert
        capturedEmailBody.Should().NotBeNull();
        var allText = string.Join(" ", capturedEmailBody!.SelectMany(t => t.Item2));
        allText.Should().Contain("https://spamma.io/logging-in?token=");
    }

    [Fact]
    public async Task Process_WhenTokenGenerationFails_DoesNotSendEmail()
    {
        // Arrange
        var ev = new AuthenticationStartedIntegrationEvent(
            UserId: Guid.NewGuid(),
            SecurityStamp: Guid.NewGuid(),
            AuthenticationAttemptId: Guid.NewGuid(),
            Name: "Test User",
            EmailAddress: "test@example.com",
            WhenHappened: DateTime.UtcNow);

        _authTokenProviderMock
            .Setup(x => x.GenerateAuthenticationToken(It.IsAny<IAuthTokenProvider.AuthenticationTokenModel>()))
            .Returns(Result.Fail<string>());

        // Act
        await _subscriber.Process(ev);

        // Assert
        _emailSenderMock.Verify(
            x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Process_WhenTokenGenerationSucceeds_EmailContainsUserName()
    {
        // Arrange
        var ev = new AuthenticationStartedIntegrationEvent(
            UserId: Guid.NewGuid(),
            SecurityStamp: Guid.NewGuid(),
            AuthenticationAttemptId: Guid.NewGuid(),
            Name: "Alice Wonder",
            EmailAddress: "alice@example.com",
            WhenHappened: DateTime.UtcNow);

        _authTokenProviderMock
            .Setup(x => x.GenerateAuthenticationToken(It.IsAny<IAuthTokenProvider.AuthenticationTokenModel>()))
            .Returns(Result.Ok("token"));

        List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>? capturedEmailBody = null;

        _emailSenderMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>, CancellationToken>(
                (_, _, _, body, _) => capturedEmailBody = body)
            .ReturnsAsync(Result.Ok());

        // Act
        await _subscriber.Process(ev);

        // Assert
        capturedEmailBody.Should().NotBeNull();
        var allText = string.Join(" ", capturedEmailBody!.SelectMany(t => t.Item2));
        allText.Should().Contain("Alice Wonder");
    }
}
