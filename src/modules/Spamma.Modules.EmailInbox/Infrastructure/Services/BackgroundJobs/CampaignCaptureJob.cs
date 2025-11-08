namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public sealed record CampaignCaptureJob(
    Guid DomainId,
    Guid SubdomainId,
    Guid EmailId,
    string CampaignValue,
    string Subject,
    string FromAddress,
    string ToAddress,
    DateTimeOffset CapturedAt);

