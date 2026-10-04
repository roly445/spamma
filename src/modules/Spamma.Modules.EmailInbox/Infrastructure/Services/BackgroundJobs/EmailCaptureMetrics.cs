using System.Diagnostics.Metrics;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

internal static class EmailCaptureMetrics
{
    internal static readonly Meter Meter = new("Spamma.EmailCapture");

    internal static readonly Counter<long> QueueFailures = Meter.CreateCounter<long>("spamma.smtp.capture.queue_failures");

    internal static readonly Counter<long> ProcessingFailures = Meter.CreateCounter<long>("spamma.smtp.capture.processing_failures");
}
