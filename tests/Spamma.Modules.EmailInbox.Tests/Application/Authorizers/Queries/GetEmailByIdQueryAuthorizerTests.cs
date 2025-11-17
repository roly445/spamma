using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

public class GetEmailByIdQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new GetEmailByIdQuery(Guid.NewGuid());
        var authorizer = new GetEmailByIdQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsSubdomainAccessViaEmailRequirement()
    {
        // Arrange
        var query = new GetEmailByIdQuery(Guid.NewGuid());
        var authorizer = new GetEmailByIdQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToSubdomainViaEmailRequirement");
    }

    [Fact]
    public void BuildPolicy_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var query = new GetEmailByIdQuery(Guid.NewGuid());
        var authorizer = new GetEmailByIdQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentEmailIds()
    {
        // Arrange
        var emailId1 = Guid.NewGuid();
        var emailId2 = Guid.NewGuid();
        var authorizer1 = new GetEmailByIdQueryAuthorizer();
        var authorizer2 = new GetEmailByIdQueryAuthorizer();

        // Act
        authorizer1.BuildPolicy(new GetEmailByIdQuery(emailId1));
        authorizer2.BuildPolicy(new GetEmailByIdQuery(emailId2));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}