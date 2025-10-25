using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ResultMonad;
using MaybeMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Tests.Builders;
using Spamma.Modules.DomainManagement.Tests.Fixtures;
using BluQube.Commands;
using FluentValidation;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Subdomain;

public class SuspendSubdomainCommandHandlerTests
{
    private readonly Mock<ISubdomainRepository> _repositoryMock;
    private readonly Mock<ILogger<SuspendSubdomainCommandHandler>> _loggerMock;
    private readonly SuspendSubdomainCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public SuspendSubdomainCommandHandlerTests()
    {
        _repositoryMock = new Mock<ISubdomainRepository>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<SuspendSubdomainCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var validators = Array.Empty<IValidator<SuspendSubdomainCommand>>();

        _handler = new SuspendSubdomainCommandHandler(
            _repositoryMock.Object,
            _timeProvider,
            validators,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSubdomainFound_SuspendsSuccessfully()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        var subdomain = new SubdomainBuilder()
            .WithId(subdomainId)
            .WithDomainId(domainId)
            .WithName("mail")
            .Build();

        var command = new SuspendSubdomainCommand(subdomainId, SubdomainSuspensionReason.PolicyViolation, "Test suspension");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(subdomainId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(subdomain));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(subdomainId, CancellationToken.None),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(s => s.Id == subdomainId),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSubdomainNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var command = new SuspendSubdomainCommand(subdomainId, SubdomainSuspensionReason.PolicyViolation, "Test suspension");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(subdomainId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>.Nothing);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithDifferentSuspensionReasons_SuspendsEach()
    {
        // Arrange
        var subdomainId1 = Guid.NewGuid();
        var subdomainId2 = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        var subdomain1 = new SubdomainBuilder()
            .WithId(subdomainId1)
            .WithDomainId(domainId)
            .WithName("mail")
            .Build();

        var subdomain2 = new SubdomainBuilder()
            .WithId(subdomainId2)
            .WithDomainId(domainId)
            .WithName("ftp")
            .Build();

        var command1 = new SuspendSubdomainCommand(subdomainId1, SubdomainSuspensionReason.PolicyViolation, "Policy violation");
        var command2 = new SuspendSubdomainCommand(subdomainId2, SubdomainSuspensionReason.SecurityConcern, "Security concern");

        _repositoryMock
            .SetupSequence(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.From(subdomain1))
            .ReturnsAsync(Maybe.From(subdomain2));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Verify
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
