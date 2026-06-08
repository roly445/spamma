using System.Net;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Spamma.App.Client.Components.UserControls;
using Spamma.App.Client.Infrastructure.Constants;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Xunit;

namespace Spamma.App.Tests;

public class EmailViewerTests : BunitContext
{
    [Fact]
    public void Render_WhenMimeRequestReturnsNotFound_ShowsErrorState()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var notifications = new RecordingNotificationService();

        Services.AddSingleton(new HttpClient(new StatusCodeMessageHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("http://localhost/"),
        });
        Services.AddSingleton(new Mock<BluQube.Commands.ICommandRunner>().Object);
        Services.AddSingleton<INotificationService>(notifications);
        Services.AddSingleton<IClientSessionContext>(new TestClientSessionContext());

        // Act
        var cut = Render<EmailViewer>(parameters => parameters
            .Add(x => x.Email, CreateEmailSummary(emailId)));

        // Verify
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Unable to load email");
            cut.Markup.Should().Contain("stored message content could not be found");
            cut.Markup.Should().NotContain("Loading email...");
        });

        notifications.ErrorMessages.Should().ContainSingle()
            .Which.Should().Contain("stored message content could not be found");
    }

    [Fact]
    public void Render_WhenMimePayloadIsInvalid_ShowsErrorState()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var notifications = new RecordingNotificationService();

        Services.AddSingleton(new HttpClient(new ByteArrayMessageHandler(new byte[] { 1, 2, 3, 4 }))
        {
            BaseAddress = new Uri("http://localhost/"),
        });
        Services.AddSingleton(new Mock<BluQube.Commands.ICommandRunner>().Object);
        Services.AddSingleton<INotificationService>(notifications);
        Services.AddSingleton<IClientSessionContext>(new TestClientSessionContext());

        // Act
        var cut = Render<EmailViewer>(parameters => parameters
            .Add(x => x.Email, CreateEmailSummary(emailId)));

        // Verify
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Unable to load email");
            cut.Markup.Should().Contain("message content may be missing or unreadable");
            cut.Markup.Should().NotContain("Loading email...");
        });

        notifications.ErrorMessages.Should().ContainSingle()
            .Which.Should().Contain("message content may be missing or unreadable");
    }

    private static SearchEmailsQueryResult.EmailSummary CreateEmailSummary(Guid emailId)
        => new(emailId, "Subject", "user@example.com", DateTimeOffset.UtcNow, false, null, null);

    private sealed class StatusCodeMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class ByteArrayMessageHandler(byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content),
            });
    }

    private sealed class RecordingNotificationService : INotificationService
    {
        public event Action? OnChange;

        public IReadOnlyList<Notification> Notifications => [];

        public List<string> ErrorMessages { get; } = [];

        public void ShowSuccess(string message, int? durationMs = null)
            => this.OnChange?.Invoke();

        public void ShowInfo(string message, int? durationMs = null)
            => this.OnChange?.Invoke();

        public void ShowWarning(string message, int? durationMs = null)
            => this.OnChange?.Invoke();

        public void ShowError(string message, int? durationMs = null)
        {
            this.ErrorMessages.Add(message);
            this.OnChange?.Invoke();
        }

        public void Show(string message, NotificationType type, int? durationMs = null, bool autoHide = true)
            => this.OnChange?.Invoke();

        public void Remove(Guid notificationId)
            => this.OnChange?.Invoke();

        public void Clear()
            => this.OnChange?.Invoke();
    }

    private sealed class TestClientSessionContext : IClientSessionContext
    {
        public string CurrentRoute => "/m/catch-all";

        public Task InitializeAsync() => Task.CompletedTask;

        public Task<string> GetSessionIdAsync() => Task.FromResult("test-session");

        public Task TrackBreadcrumbAsync(string name, Dictionary<string, string?>? data = null)
            => Task.CompletedTask;
    }
}
