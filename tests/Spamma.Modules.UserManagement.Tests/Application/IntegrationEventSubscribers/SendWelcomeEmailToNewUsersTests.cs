using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ResultMonad;
using Spamma.Modules.Common;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;
using Spamma.Modules.UserManagement.Application.IntegrationEventSubscribers;

namespace Spamma.Modules.UserManagement.Tests.Application.IntegrationEventSubscribers;

public class SendWelcomeEmailToNewUsersTests
{
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<ILogger<SendWelcomeEmailToNewUsers>> _loggerMock;
    private readonly IOptions<Settings> _settings;
    private readonly SendWelcomeEmailToNewUsers _subscriber;

    public SendWelcomeEmailToNewUsersTests()
    {
        _emailSenderMock = new Mock<IEmailSender>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<SendWelcomeEmailToNewUsers>>();
        _settings = Options.Create(new Settings { BaseUri = "https://spamma.io" });
        _subscriber = new SendWelcomeEmailToNewUsers(_loggerMock.Object, _emailSenderMock.Object, _settings);
    }

    [Fact]
    public async Task Process_WhenSendWelcomeIsTrue_SendsWelcomeEmail()
    {
        // Arrange
        var ev = new UserCreatedIntegrationEvent(
            UserId: Guid.NewGuid(),
            Name: "John Doe",
            EmailAddress: "john@example.com",
            SendWelcome: true,
            WhenHappened: DateTime.UtcNow);

        _emailSenderMock
            .Setup(x => x.SendEmailAsync(
                It.Is<string>(name => name == "John Doe"),
                It.Is<string>(email => email == "john@example.com"),
                It.Is<string>(subject => subject == "Register"),
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
                "Register",
                It.IsAny<List<Tuple<EmailTemplateSection, System.Collections.Immutable.ImmutableArray<string>>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Process_WhenSendWelcomeIsTrue_EmailContainsUserName()
    {
        // Arrange
        var ev = new UserCreatedIntegrationEvent(
            UserId: Guid.NewGuid(),
            Name: "Jane Smith",
            EmailAddress: "jane@example.com",
            SendWelcome: true,
            WhenHappened: DateTime.UtcNow);

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
        allText.Should().Contain("Jane Smith");
    }

    [Fact]
    public async Task Process_WhenSendWelcomeIsTrue_EmailContainsBaseUri()
    {
        // Arrange
        var ev = new UserCreatedIntegrationEvent(
            UserId: Guid.NewGuid(),
            Name: "Test User",
            EmailAddress: "test@example.com",
            SendWelcome: true,
            WhenHappened: DateTime.UtcNow);

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
        allText.Should().Contain("https://spamma.io");
    }

    [Fact]
    public async Task Process_WhenSendWelcomeIsFalse_DoesNotSendEmail()
    {
        // Arrange
        var ev = new UserCreatedIntegrationEvent(
            UserId: Guid.NewGuid(),
            Name: "John Doe",
            EmailAddress: "john@example.com",
            SendWelcome: false,
            WhenHappened: DateTime.UtcNow);

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
    public async Task Process_WhenSendWelcomeIsFalse_LogsInformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ev = new UserCreatedIntegrationEvent(
            UserId: userId,
            Name: "John Doe",
            EmailAddress: "john@example.com",
            SendWelcome: false,
            WhenHappened: DateTime.UtcNow);

        // Act
        await _subscriber.Process(ev);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(userId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
