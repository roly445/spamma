using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

public class GetEmailByIdQueryAuthorizerTests
{
    [Fact]
    public void GetEmailByIdQueryAuthorizer_CanBeInstantiated()
    {
        // Arrange & Act
        var authorizer = new GetEmailByIdQueryAuthorizer();

        // Verify
        authorizer.Should().NotBeNull();
    }

    [Fact]
    public void GetEmailByIdQueryAuthorizer_BuildPolicyDoesNotThrow()
    {
        // Arrange
        var authorizer = new GetEmailByIdQueryAuthorizer();
        var query = new GetEmailByIdQuery(Guid.NewGuid());

        // Act & Verify
        var action = () => authorizer.BuildPolicy(query);
        action.Should().NotThrow();
    }

    [Fact]
    public void GetEmailByIdQueryAuthorizer_BuildPolicyWithDifferentEmailIds()
    {
        // Arrange
        var authorizer = new GetEmailByIdQueryAuthorizer();
        var emailId1 = Guid.NewGuid();
        var emailId2 = Guid.NewGuid();

        // Act & Verify
        var action1 = () => authorizer.BuildPolicy(new GetEmailByIdQuery(emailId1));
        var action2 = () => authorizer.BuildPolicy(new GetEmailByIdQuery(emailId2));

        action1.Should().NotThrow();
        action2.Should().NotThrow();
    }
}
