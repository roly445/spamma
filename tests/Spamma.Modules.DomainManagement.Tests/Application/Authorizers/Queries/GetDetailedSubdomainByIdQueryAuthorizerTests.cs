using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Queries;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Queries;

public class GetDetailedSubdomainByIdQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new GetDetailedSubdomainByIdQuery(Guid.NewGuid());
        var authorizer = new GetDetailedSubdomainByIdQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsSubdomainModeratorRequirement()
    {
        // Arrange
        var query = new GetDetailedSubdomainByIdQuery(Guid.NewGuid());
        var authorizer = new GetDetailedSubdomainByIdQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToSubdomainRequirement");
    }

    [Fact]
    public void BuildPolicy_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var query = new GetDetailedSubdomainByIdQuery(Guid.NewGuid());
        var authorizer = new GetDetailedSubdomainByIdQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentSubdomainIds()
    {
        // Arrange
        var subdomainId1 = Guid.NewGuid();
        var subdomainId2 = Guid.NewGuid();
        var authorizer1 = new GetDetailedSubdomainByIdQueryAuthorizer();
        var authorizer2 = new GetDetailedSubdomainByIdQueryAuthorizer();

        // Act
        authorizer1.BuildPolicy(new GetDetailedSubdomainByIdQuery(subdomainId1));
        authorizer2.BuildPolicy(new GetDetailedSubdomainByIdQuery(subdomainId2));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}