using FluentAssertions;
using Moq;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Queries;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Queries;

public class GetUserByIdQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_WhenNotInternalQuery_AddsAuthenticatedRequirement()
    {
        // Arrange
        var internalQueryStoreMock = new Mock<IInternalQueryStore>();
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);

        internalQueryStoreMock
            .Setup(x => x.IsQueryStored(query))
            .Returns(false);

        var authorizer = new GetUserByIdQueryAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_WhenNotInternalQuery_AddsUserAdministratorRequirement()
    {
        // Arrange
        var internalQueryStoreMock = new Mock<IInternalQueryStore>();
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);

        internalQueryStoreMock
            .Setup(x => x.IsQueryStored(query))
            .Returns(false);

        var authorizer = new GetUserByIdQueryAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeUserAdministratorRequirement);
    }

    [Fact]
    public void BuildPolicy_WhenNotInternalQuery_AddsExactlyTwoRequirements()
    {
        // Arrange
        var internalQueryStoreMock = new Mock<IInternalQueryStore>();
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);

        internalQueryStoreMock
            .Setup(x => x.IsQueryStored(query))
            .Returns(false);

        var authorizer = new GetUserByIdQueryAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WhenInternalQuery_AddsNoRequirements()
    {
        // Arrange
        var internalQueryStoreMock = new Mock<IInternalQueryStore>();
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);

        internalQueryStoreMock
            .Setup(x => x.IsQueryStored(query))
            .Returns(true);

        var authorizer = new GetUserByIdQueryAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().BeEmpty();
    }

    [Fact]
    public void BuildPolicy_WithDifferentUserIds_ProducesSameRequirements()
    {
        // Arrange
        var internalQueryStoreMock1 = new Mock<IInternalQueryStore>();
        var internalQueryStoreMock2 = new Mock<IInternalQueryStore>();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var query1 = new GetUserByIdQuery(userId1);
        var query2 = new GetUserByIdQuery(userId2);

        internalQueryStoreMock1
            .Setup(x => x.IsQueryStored(query1))
            .Returns(false);

        internalQueryStoreMock2
            .Setup(x => x.IsQueryStored(query2))
            .Returns(false);

        var authorizer1 = new GetUserByIdQueryAuthorizer(internalQueryStoreMock1.Object);
        var authorizer2 = new GetUserByIdQueryAuthorizer(internalQueryStoreMock2.Object);

        // Act
        authorizer1.BuildPolicy(query1);
        authorizer2.BuildPolicy(query2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeUserAdministratorRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeUserAdministratorRequirement");
    }
}