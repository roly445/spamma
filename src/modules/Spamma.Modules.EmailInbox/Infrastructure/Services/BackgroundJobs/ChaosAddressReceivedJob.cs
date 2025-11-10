namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job to record chaos address received event asynchronously.
/// Captured before scope disposal to avoid ObjectDisposedException.
/// </summary>
public sealed record ChaosAddressReceivedJob(
    Guid ChaosAddressId,
    DateTime ReceivedAt);
