using FluentAssertions;
using Moq;
using Spamma.Modules.Common;
using Spamma.Modules.DomainManagement.Application.Authorizers.Queries;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Queries;

public class SearchSubdomainsQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_WhenQueryNotStored_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new SearchSubdomainsQuery(null, null, null, 1, 10, "Name", false);
        var internalQueryStoreMock = new Mock<IInternalQueryStore>(MockBehavior.Strict);
        internalQueryStoreMock.Setup(x => x.IsQueryStored(query)).Returns(false);

        var authorizer = new SearchSubdomainsQueryAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_WhenQueryNotStored_AddsSubdomainModeratorRequirement()
    {
        // Arrange
        var query = new SearchSubdomainsQuery(null, null, null, 1, 10, "Name", false);
        var internalQueryStoreMock = new Mock<IInternalQueryStore>(MockBehavior.Strict);
        internalQueryStoreMock.Setup(x => x.IsQueryStored(query)).Returns(false);

        var authorizer = new SearchSubdomainsQueryAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToAtLeastOneSubdomainRequirement");
    }

    [Fact]
    public void BuildPolicy_WhenQueryStored_AddsNoRequirements()
    {
        // Arrange
        var query = new SearchSubdomainsQuery(null, null, null, 1, 10, "Name", false);
        var internalQueryStoreMock = new Mock<IInternalQueryStore>(MockBehavior.Strict);
        internalQueryStoreMock.Setup(x => x.IsQueryStored(query)).Returns(true);

        var authorizer = new SearchSubdomainsQueryAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(query);

        // Assert - Internal queries bypass authorization
        authorizer.Requirements.Should().BeEmpty();
    }

    [Fact]
    public void BuildPolicy_WhenQueryNotStored_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var query = new SearchSubdomainsQuery(null, null, null, 1, 10, "Name", false);
        var internalQueryStoreMock = new Mock<IInternalQueryStore>(MockBehavior.Strict);
        internalQueryStoreMock.Setup(x => x.IsQueryStored(query)).Returns(false);

        var authorizer = new SearchSubdomainsQueryAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_ChecksInternalQueryStore()
    {
        // Arrange
        var query = new SearchSubdomainsQuery(null, null, null, 1, 10, "Name", false);
        var internalQueryStoreMock = new Mock<IInternalQueryStore>(MockBehavior.Strict);
        internalQueryStoreMock.Setup(x => x.IsQueryStored(query)).Returns(false);

        var authorizer = new SearchSubdomainsQueryAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        internalQueryStoreMock.Verify(x => x.IsQueryStored(query), Times.Once);
    }
}