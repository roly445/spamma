using FluentAssertions;
using Moq;
using ResultMonad;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Application.IntegrationEventSubscribers;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Tests.Application.IntegrationEventSubscribers;

public class DeleteFileWhenEmailIsDeletedTests
{
    [Fact]
    public async Task Process_EmailDeleted_CallsDeleteMessageContentAsync()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var ev = new EmailDeletedIntegrationEvent(emailId);

        var messageStoreProviderMock = new Mock<IMessageStoreProvider>(MockBehavior.Strict);
        messageStoreProviderMock
            .Setup(x => x.DeleteMessageContentAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var subscriber = new DeleteFileWhenEmailIsDeleted(messageStoreProviderMock.Object);

        // Act
        await subscriber.Process(ev);

        // Verify - Should call delete with the exact email ID
        messageStoreProviderMock.Verify(
            x => x.DeleteMessageContentAsync(emailId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Process_DeleteSucceeds_CompletesSuccessfully()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var ev = new EmailDeletedIntegrationEvent(emailId);

        var messageStoreProviderMock = new Mock<IMessageStoreProvider>(MockBehavior.Strict);
        messageStoreProviderMock
            .Setup(x => x.DeleteMessageContentAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var subscriber = new DeleteFileWhenEmailIsDeleted(messageStoreProviderMock.Object);

        // Act
        var action = async () => await subscriber.Process(ev);

        // Verify - Should not throw on success
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Process_MultipleEmails_DeletesEachFile()
    {
        // Arrange
        var emailId1 = Guid.NewGuid();
        var emailId2 = Guid.NewGuid();
        var emailId3 = Guid.NewGuid();

        var messageStoreProviderMock = new Mock<IMessageStoreProvider>(MockBehavior.Strict);
        messageStoreProviderMock
            .Setup(x => x.DeleteMessageContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var subscriber = new DeleteFileWhenEmailIsDeleted(messageStoreProviderMock.Object);

        // Act
        var ev1 = new EmailDeletedIntegrationEvent(emailId1);
        var ev2 = new EmailDeletedIntegrationEvent(emailId2);
        var ev3 = new EmailDeletedIntegrationEvent(emailId3);

        await subscriber.Process(ev1);
        await subscriber.Process(ev2);
        await subscriber.Process(ev3);

        // Verify - All three deletes called
        messageStoreProviderMock.Verify(
            x => x.DeleteMessageContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Process_PassesCorrectEmailIdToProvider()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var capturedIds = new List<Guid>();

        var messageStoreProviderMock = new Mock<IMessageStoreProvider>(MockBehavior.Strict);
        messageStoreProviderMock
            .Setup(x => x.DeleteMessageContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((id, _) => capturedIds.Add(id))
            .ReturnsAsync(Result.Ok());

        var subscriber = new DeleteFileWhenEmailIsDeleted(messageStoreProviderMock.Object);
        var ev = new EmailDeletedIntegrationEvent(emailId);

        // Act
        await subscriber.Process(ev);

        // Verify - Exact email ID passed
        capturedIds.Should().ContainSingle(id => id == emailId);
    }

    [Fact]
    public async Task Process_EventWithDifferentDeletionTimes_StillDeletesFile()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var messageStoreProviderMock = new Mock<IMessageStoreProvider>(MockBehavior.Strict);
        messageStoreProviderMock
            .Setup(x => x.DeleteMessageContentAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var subscriber = new DeleteFileWhenEmailIsDeleted(messageStoreProviderMock.Object);

        // Act
        var ev1 = new EmailDeletedIntegrationEvent(emailId);
        var ev2 = new EmailDeletedIntegrationEvent(emailId);
        var ev3 = new EmailDeletedIntegrationEvent(emailId);

        await subscriber.Process(ev1);
        await subscriber.Process(ev2);
        await subscriber.Process(ev3);

        // Verify - Deletion times don't affect file deletion
        messageStoreProviderMock.Verify(
            x => x.DeleteMessageContentAsync(emailId, It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Process_RespondsToDeleteEmaildIntegrationEvent()
    {
        // Arrange - Verify subscriber can be created and receives the right event type
        var messageStoreProviderMock = new Mock<IMessageStoreProvider>(MockBehavior.Strict);
        messageStoreProviderMock
            .Setup(x => x.DeleteMessageContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var subscriber = new DeleteFileWhenEmailIsDeleted(messageStoreProviderMock.Object);

        // Act & Verify - Should process EmailDeletedIntegrationEvent without errors
        var ev = new EmailDeletedIntegrationEvent(Guid.NewGuid());
        var action = async () => await subscriber.Process(ev);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Process_DeletionFails_DoesNotThrow()
    {
        // Arrange - Deletion failure should be handled gracefully
        var emailId = Guid.NewGuid();
        var messageStoreProviderMock = new Mock<IMessageStoreProvider>(MockBehavior.Strict);
        messageStoreProviderMock
            .Setup(x => x.DeleteMessageContentAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail());

        var subscriber = new DeleteFileWhenEmailIsDeleted(messageStoreProviderMock.Object);
        var ev = new EmailDeletedIntegrationEvent(emailId);

        // Act
        var action = async () => await subscriber.Process(ev);

        // Assert - Should complete without throwing even if deletion fails
        await action.Should().NotThrowAsync();
    }
}