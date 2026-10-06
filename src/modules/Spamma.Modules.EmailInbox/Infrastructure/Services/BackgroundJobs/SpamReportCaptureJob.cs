namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public sealed record SpamReportCaptureJob(
    Stream MimeStream, Guid DomainId, Guid SubdomainId, Guid ChaosAddressId, Guid MessageId) : IBaseEmailCaptureJob;
