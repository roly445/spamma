using FluentAssertions;
using Marten;
using Moq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

public class EmailAccessAuthorizerTests
{
    [Fact]
    public async Task CanAccessAsync_DeletedMessage_DeniesAssignedUser()
    {
        var subdomainId = Guid.NewGuid();
        var user = UserAuthInfo.Authenticated(Guid.NewGuid(), "Test", "test@example.test", 0, [], [], [subdomainId]);
        var email = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
            DeletedAt = DateTime.UtcNow,
        };

        var result = await EmailAccessAuthorizer.CanAccessAsync(
            user, email, Mock.Of<IDocumentSession>(), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessAsync_ActiveMessage_AllowsAssignedUser()
    {
        var subdomainId = Guid.NewGuid();
        var user = UserAuthInfo.Authenticated(Guid.NewGuid(), "Test", "test@example.test", 0, [], [], [subdomainId]);
        var email = new EmailLookup
        {
            Id = Guid.NewGuid(),
            SubdomainId = subdomainId,
        };

        var result = await EmailAccessAuthorizer.CanAccessAsync(
            user, email, Mock.Of<IDocumentSession>(), CancellationToken.None);

        result.Should().BeTrue();
    }
}
