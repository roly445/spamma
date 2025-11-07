namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job to record campaign capture asynchronously.
/// Captured before scope disposal to avoid ObjectDisposedException.
/// </summary>
public sealed record CampaignCaptureJob(
    Guid DomainId,
    Guid SubdomainId,
    Guid EmailId,
    string CampaignValue,
    string Subject,
    string FromAddress,
    string ToAddress,
    DateTimeOffset CapturedAt);

