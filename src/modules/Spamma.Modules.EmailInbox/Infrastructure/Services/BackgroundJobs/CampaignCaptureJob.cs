namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public sealed record CampaignCaptureJob(
    Stream MimeStream,
    Guid DomainId,
    Guid SubdomainId) : IBaseEmailCaptureJob;