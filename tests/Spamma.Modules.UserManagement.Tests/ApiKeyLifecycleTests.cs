using System.Security.Claims;
using BluQube.Commands;
using BluQube.Constants;
using FluentAssertions;
using Marten;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.ApiKey;
using Spamma.Modules.UserManagement.Application.CommandHandlers.ApiKeys;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;
using Spamma.Modules.UserManagement.Infrastructure.Repositories;
using Spamma.Modules.UserManagement.Tests.Fixtures;
using Spamma.Modules.UserManagement.Tests.Integration;

namespace Spamma.Modules.UserManagement.Tests;

public class ApiKeyLifecycleTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public ApiKeyLifecycleTests(PostgreSqlFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task CreateApiKeyCommand_IntegrationWithRepository_Succeeds()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDocumentStore>(this._fixture.Store!);
        services.AddScoped<IDocumentSession>(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        var userId = Guid.NewGuid();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        var identityMock = new Mock<System.Security.Principal.IIdentity>();
        identityMock.Setup(x => x.IsAuthenticated).Returns(true);

        var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Identity).Returns(identityMock.Object);
        claimsPrincipalMock.Setup(x => x.FindFirst(ClaimTypes.NameIdentifier))
            .Returns(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        services.AddSingleton<IHttpContextAccessor>(httpContextAccessorMock.Object);
        services.AddSingleton<TimeProvider>(new StubTimeProvider(new DateTime(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc)));

        var eventPublisherMock = new Mock<IIntegrationEventPublisher>();
        eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<ApiKeyCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        services.AddSingleton<IIntegrationEventPublisher>(eventPublisherMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var handler = ActivatorUtilities.CreateInstance<CreateApiKeyCommandHandler>(serviceProvider);
        var command = new CreateApiKeyCommand("Integration Test API Key", DateTime.UtcNow.AddDays(30));

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(CommandResultStatus.Succeeded);
        result.Data.Should().NotBeNull();
        result.Data.ApiKey.Should().NotBeNull();
        result.Data.ApiKey.Name.Should().Be("Integration Test API Key");
        result.Data.ApiKey.IsRevoked.Should().BeFalse();
        result.Data.KeyValue.Should().NotBeNullOrEmpty();
        result.Data.KeyValue.Should().StartWith("sk-");

        eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<ApiKeyCreatedIntegrationEvent>(e =>
                    e.UserId == userId &&
                    e.ApiKeyId == result.Data.ApiKey.Id &&
                    e.Name == "Integration Test API Key"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
