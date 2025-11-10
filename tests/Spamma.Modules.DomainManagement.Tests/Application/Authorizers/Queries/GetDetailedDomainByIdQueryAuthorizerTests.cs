using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Queries;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Queries;

/// <summary>
/// Tests for GetDetailedDomainByIdQueryAuthorizer to verify authorization requirements.
/// </summary>
public class GetDetailedDomainByIdQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new GetDetailedDomainByIdQuery(Guid.NewGuid());
        var authorizer = new GetDetailedDomainByIdQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsDomainModeratorRequirement()
    {
        // Arrange
        var query = new GetDetailedDomainByIdQuery(Guid.NewGuid());
        var authorizer = new GetDetailedDomainByIdQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToDomainRequirement");
    }

    [Fact]
    public void BuildPolicy_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var query = new GetDetailedDomainByIdQuery(Guid.NewGuid());
        var authorizer = new GetDetailedDomainByIdQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentDomainIds()
    {
        // Arrange
        var domainId1 = Guid.NewGuid();
        var domainId2 = Guid.NewGuid();
        var authorizer1 = new GetDetailedDomainByIdQueryAuthorizer();
        var authorizer2 = new GetDetailedDomainByIdQueryAuthorizer();

        // Act
        authorizer1.BuildPolicy(new GetDetailedDomainByIdQuery(domainId1));
        authorizer2.BuildPolicy(new GetDetailedDomainByIdQuery(domainId2));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}
