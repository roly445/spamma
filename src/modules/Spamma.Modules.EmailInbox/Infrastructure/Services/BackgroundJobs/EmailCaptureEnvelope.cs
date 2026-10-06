namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public enum EmailCaptureKind
{
    Standard,
    Campaign,
    CatchAll,
    Chaos,
    SpamReport,
}

public record EmailCaptureEnvelope(
    Guid MessageId,
    EmailCaptureKind Kind,
    byte[] MimeContent,
    Guid DomainId,
    Guid SubdomainId,
    Guid? ChaosAddressId = null,
    Guid? CatchAllSenderAddressId = null,
    string? CampaignValue = null)
{
    public IBaseEmailCaptureJob ToJob()
    {
        var stream = new MemoryStream(this.MimeContent, writable: false);
        return this.Kind switch
        {
            EmailCaptureKind.Standard => new StandardEmailCaptureJob(stream, this.DomainId, this.SubdomainId, this.MessageId),
            EmailCaptureKind.Campaign => new CampaignCaptureJob(stream, this.DomainId, this.SubdomainId, this.MessageId),
            EmailCaptureKind.CatchAll => new CatchAllEmailCaptureJob(stream, this.DomainId, this.SubdomainId, this.MessageId, this.CatchAllSenderAddressId, this.CampaignValue),
            EmailCaptureKind.Chaos when this.ChaosAddressId.HasValue => new ChaosEmailCaptureJob(stream, this.DomainId, this.SubdomainId, this.ChaosAddressId.Value, this.MessageId),
            EmailCaptureKind.SpamReport when this.ChaosAddressId.HasValue => new SpamReportCaptureJob(stream, this.DomainId, this.SubdomainId, this.ChaosAddressId.Value, this.MessageId),
            _ => throw new InvalidOperationException($"Invalid email capture job {this.MessageId} ({this.Kind})."),
        };
    }
}
