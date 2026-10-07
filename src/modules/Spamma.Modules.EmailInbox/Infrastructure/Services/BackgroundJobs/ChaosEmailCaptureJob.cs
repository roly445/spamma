namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public sealed record ChaosEmailCaptureJob(
    Stream MimeStream, Guid DomainId, Guid SubdomainId, Guid ChaosAddressId, Guid MessageId = default,
    int SmtpCode = 0, string Recipient = "") : IBaseEmailCaptureJob;
