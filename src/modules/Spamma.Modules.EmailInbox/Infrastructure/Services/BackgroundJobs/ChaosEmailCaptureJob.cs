namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public sealed record ChaosEmailCaptureJob(
    Stream MimeStream, Guid DomainId, Guid SubdomainId, Guid ChaosAddressId) : IBaseEmailCaptureJob;