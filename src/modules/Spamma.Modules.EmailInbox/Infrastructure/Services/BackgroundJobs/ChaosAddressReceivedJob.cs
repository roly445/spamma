namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public sealed record ChaosAddressReceivedJob(
    Guid ChaosAddressId,
    DateTime ReceivedAt);