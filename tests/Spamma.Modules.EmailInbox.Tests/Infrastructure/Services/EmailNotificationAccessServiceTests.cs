using Marten;
using Microsoft.Extensions.Options;
using Moq;
using Spamma.Modules.Common;
using Spamma.Modules.EmailInbox.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class EmailNotificationAccessServiceTests
{
    [Fact]
    public async Task CanAccessNotificationAsync_UsesCurrentOwnerSubdomainPermissions()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var firstSubdomainId = Guid.NewGuid();
        var secondSubdomainId = Guid.NewGuid();
        var first = new UserLookup { Id = firstUserId, ViewableSubdomains = [firstSubdomainId] };
        var second = new UserLookup { Id = secondUserId, ViewableSubdomains = [secondSubdomainId] };
        var session = new Mock<IDocumentSession>();
        session.SetupSequence(x => x.LoadAsync<UserLookup>(firstUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(first)
            .ReturnsAsync(new UserLookup { Id = firstUserId });
        session.Setup(x => x.LoadAsync<UserLookup>(secondUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(second);
        var service = new EmailNotificationAccessService(session.Object, Options.Create(new Settings()));
        var email = new PushNotificationManager.EmailDetails(
            Guid.NewGuid(), firstSubdomainId, "sender@example.com", "first@example.com", "First", "Body",
            DateTimeOffset.UtcNow, DomainId: Guid.NewGuid());

        Assert.True(await service.CanAccessNotificationAsync(firstUserId, email, CancellationToken.None));
        Assert.False(await service.CanAccessNotificationAsync(secondUserId, email, CancellationToken.None));

        Assert.False(await service.CanAccessNotificationAsync(firstUserId, email, CancellationToken.None));
    }

    [Fact]
    public async Task CanAccessNotificationAsync_CatchAllRequiresAssignedSender()
    {
        var assignedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<UserLookup>(assignedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserLookup { Id = assignedUserId });
        session.Setup(x => x.LoadAsync<UserLookup>(otherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserLookup { Id = otherUserId });
        session.Setup(x => x.LoadAsync<CatchAllSenderAddressLookup>(senderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatchAllSenderAddressLookup { Id = senderId, AssignedUserIds = [assignedUserId] });
        var service = new EmailNotificationAccessService(session.Object, Options.Create(new Settings()));
        var email = new PushNotificationManager.EmailDetails(
            Guid.NewGuid(), CatchAllConstants.SubdomainId, "sender@example.com", "unknown@example.com",
            "Catch-all", "Body", DateTimeOffset.UtcNow, IsCatchAll: true,
            DomainId: CatchAllConstants.DomainId, CatchAllSenderAddressId: senderId);

        Assert.True(await service.CanAccessNotificationAsync(assignedUserId, email, CancellationToken.None));
        Assert.False(await service.CanAccessNotificationAsync(otherUserId, email, CancellationToken.None));
    }

    [Fact]
    public async Task CanAccessEmailAsync_RejectsOtherOwnersEmail()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<UserLookup>(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserLookup { Id = ownerId, ViewableSubdomains = [subdomainId] });
        session.Setup(x => x.LoadAsync<UserLookup>(otherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserLookup { Id = otherUserId });
        session.Setup(x => x.LoadAsync<EmailLookup>(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailLookup { Id = emailId, DomainId = Guid.NewGuid(), SubdomainId = subdomainId });
        var service = new EmailNotificationAccessService(session.Object, Options.Create(new Settings()));

        Assert.True(await service.CanAccessEmailAsync(ownerId, emailId, CancellationToken.None));
        Assert.False(await service.CanAccessEmailAsync(otherUserId, emailId, CancellationToken.None));
    }
}
