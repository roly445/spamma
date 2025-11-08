using MimeKit;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job for processing ingested email: file storage and command execution.
/// </summary>
public sealed record EmailIngestionJob(
    Guid MessageId,
    MimeMessage Message,
    Guid ParentDomainId,
    Guid SubdomainId,
    Guid? ChaosAddressId,
    bool IsCampaignEmail);
