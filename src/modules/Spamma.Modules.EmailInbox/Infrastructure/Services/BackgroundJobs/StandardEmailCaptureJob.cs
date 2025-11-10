namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public sealed record StandardEmailCaptureJob(
    Stream MimeStream, Guid DomainId, Guid SubdomainId, Guid MessageId) : IBaseEmailCaptureJob;