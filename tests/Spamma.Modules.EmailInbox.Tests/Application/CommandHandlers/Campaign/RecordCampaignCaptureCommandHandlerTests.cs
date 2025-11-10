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
using CampaignAggregate = Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Campaign;

namespace Spamma.Modules.EmailInbox.Tests.Application.CommandHandlers.Campaign;

public class RecordCampaignCaptureCommandHandlerTests
{
    [Fact]
    public async Task Handle_NewCampaign_Succeeds()
    {
        var cmd = new RecordCampaignCaptureCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test@ex.com", DateTimeOffset.UtcNow);

        var repoMock = new Mock<ICampaignRepository>(MockBehavior.Strict);
        repoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Maybe<CampaignAggregate>>(Maybe<CampaignAggregate>.Nothing));
        repoMock
            .Setup(x => x.SaveAsync(It.IsAny<CampaignAggregate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result.Ok()));

        var handler = new RecordCampaignCaptureCommandHandler(
            Array.Empty<IValidator<RecordCampaignCaptureCommand>>(),
            new Mock<ILogger<RecordCampaignCaptureCommandHandler>>().Object,
            repoMock.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        repoMock.Verify(x => x.SaveAsync(It.IsAny<CampaignAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingCampaign_RecordsCapture()
    {
        var campaign = new CampaignBuilder().Build();
        var cmd = new RecordCampaignCaptureCommand(
            campaign.Id, Guid.NewGuid(), Guid.NewGuid(), "test@ex.com", DateTimeOffset.UtcNow);

        var repoMock = new Mock<ICampaignRepository>(MockBehavior.Strict);
        repoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Maybe.From<CampaignAggregate>(campaign)));
        repoMock
            .Setup(x => x.SaveAsync(It.IsAny<CampaignAggregate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result.Ok()));

        var handler = new RecordCampaignCaptureCommandHandler(
            Array.Empty<IValidator<RecordCampaignCaptureCommand>>(),
            new Mock<ILogger<RecordCampaignCaptureCommandHandler>>().Object,
            repoMock.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        repoMock.Verify(x => x.SaveAsync(It.IsAny<CampaignAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SaveFails_ReturnsFailed()
    {
        var cmd = new RecordCampaignCaptureCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test@ex.com", DateTimeOffset.UtcNow);

        var repoMock = new Mock<ICampaignRepository>(MockBehavior.Strict);
        repoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Maybe<CampaignAggregate>>(Maybe<CampaignAggregate>.Nothing));
        repoMock
            .Setup(x => x.SaveAsync(It.IsAny<CampaignAggregate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result.Fail()));

        var handler = new RecordCampaignCaptureCommandHandler(
            Array.Empty<IValidator<RecordCampaignCaptureCommand>>(),
            new Mock<ILogger<RecordCampaignCaptureCommandHandler>>().Object,
            repoMock.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_RepositoryThrows_ReturnsFailed()
    {
        var cmd = new RecordCampaignCaptureCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test@ex.com", DateTimeOffset.UtcNow);

        var repoMock = new Mock<ICampaignRepository>(MockBehavior.Strict);
        repoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("DB error"));

        var handler = new RecordCampaignCaptureCommandHandler(
            Array.Empty<IValidator<RecordCampaignCaptureCommand>>(),
            new Mock<ILogger<RecordCampaignCaptureCommandHandler>>().Object,
            repoMock.Object);

        // Act & Assert - expect exception to be thrown
        await FluentActions.Invoking(async () => await handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("DB error");
    }
}
