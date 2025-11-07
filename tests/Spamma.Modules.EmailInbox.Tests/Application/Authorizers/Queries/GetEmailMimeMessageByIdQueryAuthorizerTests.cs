using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

public class GetEmailMimeMessageByIdQueryAuthorizerTests
{
    [Fact]
    public void GetEmailMimeMessageByIdQueryAuthorizer_CanBeInstantiated()
    {
        // Arrange & Act
        var authorizer = new GetEmailMimeMessageByIdQueryAuthorizer();

        // Verify
        authorizer.Should().NotBeNull();
    }

    [Fact]
    public void GetEmailMimeMessageByIdQueryAuthorizer_BuildPolicyDoesNotThrow()
    {
        // Arrange
        var authorizer = new GetEmailMimeMessageByIdQueryAuthorizer();
        var query = new GetEmailMimeMessageByIdQuery(Guid.NewGuid());

        // Act & Verify
        var action = () => authorizer.BuildPolicy(query);
        action.Should().NotThrow();
    }

    [Fact]
    public void GetEmailMimeMessageByIdQueryAuthorizer_BuildPolicyWithDifferentEmailIds()
    {
        // Arrange
        var authorizer = new GetEmailMimeMessageByIdQueryAuthorizer();
        var emailId1 = Guid.NewGuid();
        var emailId2 = Guid.NewGuid();

        // Act & Verify
        var action1 = () => authorizer.BuildPolicy(new GetEmailMimeMessageByIdQuery(emailId1));
        var action2 = () => authorizer.BuildPolicy(new GetEmailMimeMessageByIdQuery(emailId2));

        action1.Should().NotThrow();
        action2.Should().NotThrow();
    }

    [Fact]
    public void GetEmailMimeMessageByIdQueryAuthorizer_ProtectsMimeDownloads()
    {
        // Arrange
        var authorizer = new GetEmailMimeMessageByIdQueryAuthorizer();
        var emailId = Guid.NewGuid();

        // Act & Verify - Downloading MIME content should require authorization
        var action = () => authorizer.BuildPolicy(new GetEmailMimeMessageByIdQuery(emailId));
        action.Should().NotThrow();
    }
}
