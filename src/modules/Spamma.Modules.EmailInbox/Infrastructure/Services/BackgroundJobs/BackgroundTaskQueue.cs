using DotNetCore.CAP;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public class BackgroundTaskQueue(ICapPublisher publisher) : IBackgroundTaskQueue
{
    public const string CaptureTopic = "spamma.smtp.capture";

    public void QueueBackgroundWorkItem(IBaseEmailCaptureJob workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        using var stream = new MemoryStream();
        workItem.MimeStream.Position = 0;
        workItem.MimeStream.CopyTo(stream);
        var content = stream.ToArray();

        var envelope = workItem switch
        {
            StandardEmailCaptureJob standard => new EmailCaptureEnvelope(
                standard.MessageId, EmailCaptureKind.Standard, content, standard.DomainId, standard.SubdomainId),
            CampaignCaptureJob campaign => new EmailCaptureEnvelope(
                campaign.MessageId == Guid.Empty ? Guid.NewGuid() : campaign.MessageId,
                EmailCaptureKind.Campaign, content, campaign.DomainId, campaign.SubdomainId),
            CatchAllEmailCaptureJob catchAll => new EmailCaptureEnvelope(
                catchAll.MessageId, EmailCaptureKind.CatchAll, content, catchAll.DomainId, catchAll.SubdomainId,
                CatchAllSenderAddressId: catchAll.CatchAllSenderAddressId, CampaignValue: catchAll.CampaignValue),
            ChaosEmailCaptureJob chaos => new EmailCaptureEnvelope(
                chaos.MessageId == Guid.Empty ? Guid.NewGuid() : chaos.MessageId,
                EmailCaptureKind.Chaos, content, chaos.DomainId, chaos.SubdomainId,
                ChaosAddressId: chaos.ChaosAddressId),
            _ => throw new ArgumentException("Unknown email capture job.", nameof(workItem)),
        };

        publisher.Publish(CaptureTopic, envelope);
        workItem.MimeStream.Dispose();
    }
}
