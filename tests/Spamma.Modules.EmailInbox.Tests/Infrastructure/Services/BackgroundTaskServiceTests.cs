using BluQube.Commands;
using FluentAssertions;
using MimeKit;
using Moq;
using ResultMonad;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class BackgroundTaskServiceTests
{
    [Fact]
    public async Task ProcessWorkItemAsync_CatchAllCampaignJob_RecordsCaptureAndStoresCatchAllEmailWithCampaignBinding()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var senderAddressId = Guid.NewGuid();
        var message = new MimeMessage
        {
            Subject = "Catch-all campaign",
            From = { new MailboxAddress("sender", "sender@test.com") },
            To = { new MailboxAddress("recipient", "recipient@unknown.com") },
        };
        message.Headers.Add("x-spamma-camp", "catch-all-campaign");

        var campaignId = Guid.NewGuid();
        var commanderMock = new Mock<ICommandRunner>(MockBehavior.Strict);
        commanderMock
            .Setup(x => x.Send(
                It.Is<RecordCampaignCaptureCommand>(command =>
                    command.DomainId == CatchAllConstants.DomainId &&
                    command.SubdomainId == CatchAllConstants.SubdomainId &&
                    command.MessageId == messageId &&
                    command.CampaignValue == "catch-all-campaign"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<RecordCampaignCaptureCommandResult>.Succeeded(
                new RecordCampaignCaptureCommandResult(campaignId, true)));

        CampaignEmailReceivedCommand? receivedEmailCommand = null;
        commanderMock
            .Setup(x => x.Send(It.IsAny<CampaignEmailReceivedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CampaignEmailReceivedCommand, CancellationToken>((command, _) => receivedEmailCommand = command)
            .ReturnsAsync(CommandResult.Succeeded());

        var messageStoreProviderMock = new Mock<IMessageStoreProvider>(MockBehavior.Strict);
        messageStoreProviderMock
            .Setup(x => x.StoreMessageContentAsync(messageId, It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var job = new CatchAllEmailCaptureJob(
            CreateStream(message),
            CatchAllConstants.DomainId,
            CatchAllConstants.SubdomainId,
            messageId,
            senderAddressId,
            "catch-all-campaign");

        // Act
        await BackgroundTaskService.ProcessWorkItemAsync(
            job,
            commanderMock.Object,
            messageStoreProviderMock.Object,
            CancellationToken.None);

        // Assert
        commanderMock.VerifyAll();
        messageStoreProviderMock.VerifyAll();
        receivedEmailCommand.Should().NotBeNull();
        receivedEmailCommand!.EmailId.Should().Be(messageId);
        receivedEmailCommand.DomainId.Should().Be(CatchAllConstants.DomainId);
        receivedEmailCommand.SubdomainId.Should().Be(CatchAllConstants.SubdomainId);
        receivedEmailCommand.CampaignId.Should().Be(campaignId);
        receivedEmailCommand.CatchAllSenderAddressId.Should().Be(senderAddressId);
    }

    private static MemoryStream CreateStream(MimeMessage message)
    {
        var stream = new MemoryStream();
        message.WriteTo(stream);
        stream.Position = 0;
        return stream;
    }
}
