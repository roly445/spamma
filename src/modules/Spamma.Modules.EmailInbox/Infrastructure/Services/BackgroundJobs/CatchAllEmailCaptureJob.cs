namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public sealed record CatchAllEmailCaptureJob(
    Stream MimeStream, Guid DomainId, Guid SubdomainId, Guid MessageId) : IBaseEmailCaptureJob;
