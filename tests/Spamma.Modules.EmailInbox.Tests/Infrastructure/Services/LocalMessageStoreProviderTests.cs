using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using ResultMonad;
using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class LocalMessageStoreProviderTests
{
    [Fact]
    public async Task StoreMessageContentAsync_DirectoryExists_CreatesEmlFile()
    {
        // Arrange - use real temp directory for this test
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var hostEnvironmentMock = new Mock<IHostEnvironment>();
            hostEnvironmentMock.Setup(x => x.ContentRootPath).Returns(tempDir);

            var provider = new LocalMessageStoreProvider(
                hostEnvironmentMock.Object,
                new Mock<ILogger<LocalMessageStoreProvider>>().Object,
                new DirectoryWrapperImpl(),
                new FileWrapperImpl());

            var messageId = Guid.NewGuid();
            var message = new MimeMessage
            {
                Subject = "Test Email",
                From = { new MailboxAddress("sender", "sender@example.com") }
            };

            // Act
            var result = await provider.StoreMessageContentAsync(messageId, message);

            // Verify
            result.IsSuccess.Should().BeTrue();
            var filePath = Path.Combine(tempDir, "messages", $"{messageId}.eml");
            File.Exists(filePath).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task StoreMessageContentAsync_DirectoryDoesNotExist_CreatesDirectoryAndFile()
    {
        // Arrange - use real temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            var hostEnvironmentMock = new Mock<IHostEnvironment>();
            hostEnvironmentMock.Setup(x => x.ContentRootPath).Returns(tempDir);

            var provider = new LocalMessageStoreProvider(
                hostEnvironmentMock.Object,
                new Mock<ILogger<LocalMessageStoreProvider>>().Object,
                new DirectoryWrapperImpl(),
                new FileWrapperImpl());

            var messageId = Guid.NewGuid();
            var message = new MimeMessage
            {
                Subject = "Test Email",
                From = { new MailboxAddress("sender", "sender@example.com") }
            };

            // Act
            var result = await provider.StoreMessageContentAsync(messageId, message);

            // Verify
            result.IsSuccess.Should().BeTrue();
            var messagesDir = Path.Combine(tempDir, "messages");
            Directory.Exists(messagesDir).Should().BeTrue();
            var filePath = Path.Combine(messagesDir, $"{messageId}.eml");
            File.Exists(filePath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task DeleteMessageContentAsync_FileExists_DeletesFile()
    {
        // Arrange - use real temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var messageId = Guid.NewGuid();
            var message = new MimeMessage
            {
                Subject = "Test Email",
                From = { new MailboxAddress("sender", "sender@example.com") }
            };

            var hostEnvironmentMock = new Mock<IHostEnvironment>();
            hostEnvironmentMock.Setup(x => x.ContentRootPath).Returns(tempDir);

            var provider = new LocalMessageStoreProvider(
                hostEnvironmentMock.Object,
                new Mock<ILogger<LocalMessageStoreProvider>>().Object,
                new DirectoryWrapperImpl(),
                new FileWrapperImpl());

            // First store a message
            await provider.StoreMessageContentAsync(messageId, message);
            var filePath = Path.Combine(tempDir, "messages", $"{messageId}.eml");
            File.Exists(filePath).Should().BeTrue();

            // Act - delete it
            var result = await provider.DeleteMessageContentAsync(messageId);

            // Verify
            result.IsSuccess.Should().BeTrue();
            File.Exists(filePath).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task DeleteMessageContentAsync_DirectoryNotExists_ReturnsFail()
    {
        // Arrange
        var hostEnvironmentMock = new Mock<IHostEnvironment>();
        hostEnvironmentMock.Setup(x => x.ContentRootPath).Returns("/nonexistent/path");

        var provider = new LocalMessageStoreProvider(
            hostEnvironmentMock.Object,
            new Mock<ILogger<LocalMessageStoreProvider>>().Object,
            new DirectoryWrapperImpl(),
            new FileWrapperImpl());

        var messageId = Guid.NewGuid();

        // Act
        var result = await provider.DeleteMessageContentAsync(messageId);

        // Verify
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task LoadMessageContentAsync_FileExists_ReturnsMessage()
    {
        // Arrange - use real temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var messageId = Guid.NewGuid();
            var originalMessage = new MimeMessage
            {
                Subject = "Test Email",
                From = { new MailboxAddress("sender", "sender@example.com") },
                To = { new MailboxAddress("recipient", "recipient@example.com") }
            };

            var hostEnvironmentMock = new Mock<IHostEnvironment>();
            hostEnvironmentMock.Setup(x => x.ContentRootPath).Returns(tempDir);

            var provider = new LocalMessageStoreProvider(
                hostEnvironmentMock.Object,
                new Mock<ILogger<LocalMessageStoreProvider>>().Object,
                new DirectoryWrapperImpl(),
                new FileWrapperImpl());

            // First store a message
            await provider.StoreMessageContentAsync(messageId, originalMessage);

            // Act
            var result = await provider.LoadMessageContentAsync(messageId);

            // Verify - result is Maybe<MimeMessage>, need to check HasValue
            result.Should().NotBeNull();
            result!.HasValue.Should().BeTrue();
            var message = result.Value;
            message.Subject.Should().Be("Test Email");
            message.From.Mailboxes.First().Address.Should().Be("sender@example.com");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task LoadMessageContentAsync_DirectoryNotExists_ReturnsEmpty()
    {
        // Arrange
        var hostEnvironmentMock = new Mock<IHostEnvironment>();
        hostEnvironmentMock.Setup(x => x.ContentRootPath).Returns("/nonexistent/path");

        var provider = new LocalMessageStoreProvider(
            hostEnvironmentMock.Object,
            new Mock<ILogger<LocalMessageStoreProvider>>().Object,
            new DirectoryWrapperImpl(),
            new FileWrapperImpl());

        var messageId = Guid.NewGuid();

        // Act
        var result = await provider.LoadMessageContentAsync(messageId);

        // Verify - Maybe<T> is not null when empty, it has HasValue = false
        result.Should().NotBeNull();
        result!.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task LoadMessageContentAsync_FileNotFound_ReturnsEmpty()
    {
        // Arrange - use real temp directory that exists but file doesn't
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var hostEnvironmentMock = new Mock<IHostEnvironment>();
            hostEnvironmentMock.Setup(x => x.ContentRootPath).Returns(tempDir);

            var provider = new LocalMessageStoreProvider(
                hostEnvironmentMock.Object,
                new Mock<ILogger<LocalMessageStoreProvider>>().Object,
                new DirectoryWrapperImpl(),
                new FileWrapperImpl());

            var nonExistentMessageId = Guid.NewGuid();

            // Act
            var result = await provider.LoadMessageContentAsync(nonExistentMessageId);

            // Verify - Maybe<T> with no value has HasValue = false
            result.Should().NotBeNull();
            result!.HasValue.Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Concrete implementation of IDirectoryWrapper for testing
    /// </summary>
    private class DirectoryWrapperImpl : IDirectoryWrapper
    {
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        public bool Exists(string path) => Directory.Exists(path);
    }

    /// <summary>
    /// Concrete implementation of IFileWrapper for testing
    /// </summary>
    private class FileWrapperImpl : IFileWrapper
    {
        public void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
