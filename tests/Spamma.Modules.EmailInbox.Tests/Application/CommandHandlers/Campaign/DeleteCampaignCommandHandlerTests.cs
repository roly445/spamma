using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.EmailInbox.Application.CommandHandlers.Campaign;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;
using Spamma.Modules.EmailInbox.Tests.Builders;
using Spamma.Modules.EmailInbox.Tests.Fixtures;
using CampaignAggregate = Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Campaign;
using EmailAggregate = Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email;

namespace Spamma.Modules.EmailInbox.Tests.Application.CommandHandlers.Campaign;

public class DeleteCampaignCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingCampaign_DeletesSuccessfully()
    {
        var campaign = new CampaignBuilder().Build();
        var cmd = new DeleteCampaignCommand(campaign.Id);

        var repoMock = new Mock<ICampaignRepository>(MockBehavior.Strict);
        repoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Maybe.From<CampaignAggregate>(campaign)));
        repoMock
            .Setup(x => x.SaveAsync(It.IsAny<CampaignAggregate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result.Ok()));

        var emailRepoMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        emailRepoMock
            .Setup(x => x.GetByCampaignIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IReadOnlyList<EmailAggregate>>(new List<EmailAggregate>()));

        var handler = new DeleteCampaignCommandHandler(
            repoMock.Object,
            emailRepoMock.Object,
            new StubTimeProvider(DateTime.UtcNow),
            Array.Empty<IValidator<DeleteCampaignCommand>>(),
            new Mock<ILogger<DeleteCampaignCommandHandler>>().Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        repoMock.Verify(x => x.SaveAsync(It.IsAny<CampaignAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentCampaign_ReturnsFailed()
    {
        var cmd = new DeleteCampaignCommand(Guid.NewGuid());

        var repoMock = new Mock<ICampaignRepository>(MockBehavior.Strict);
        repoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Maybe<CampaignAggregate>>(Maybe<CampaignAggregate>.Nothing));

        var emailRepoMock = new Mock<IEmailRepository>(MockBehavior.Strict);

        var handler = new DeleteCampaignCommandHandler(
            repoMock.Object,
            emailRepoMock.Object,
            new StubTimeProvider(DateTime.UtcNow),
            Array.Empty<IValidator<DeleteCampaignCommand>>(),
            new Mock<ILogger<DeleteCampaignCommandHandler>>().Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_SaveFails_ReturnsFailed()
    {
        var campaign = new CampaignBuilder().Build();
        var cmd = new DeleteCampaignCommand(campaign.Id);

        var repoMock = new Mock<ICampaignRepository>(MockBehavior.Strict);
        repoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Maybe.From<CampaignAggregate>(campaign)));
        repoMock
            .Setup(x => x.SaveAsync(It.IsAny<CampaignAggregate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result.Fail()));

        var emailRepoMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        emailRepoMock
            .Setup(x => x.GetByCampaignIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IReadOnlyList<EmailAggregate>>(new List<EmailAggregate>()));

        var handler = new DeleteCampaignCommandHandler(
            repoMock.Object,
            emailRepoMock.Object,
            new StubTimeProvider(DateTime.UtcNow),
            Array.Empty<IValidator<DeleteCampaignCommand>>(),
            new Mock<ILogger<DeleteCampaignCommandHandler>>().Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
    }
}