using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Spamma.App.Infrastructure.Services;

/// <summary>
/// In-memory implementation of certificate progress publisher using channels for streaming.
/// </summary>
public sealed class InMemoryCertificateProgressPublisher : ICertificateProgressPublisher
{
    private static readonly ConcurrentDictionary<string, Channel<CertificateProgressEvent>> ActiveChannels =
        new();

    /// <summary>
    /// Creates a new channel for certificate progress streaming.
    /// </summary>
    /// <param name="sessionId">Unique session identifier for this certificate generation.</param>
    /// <returns>A channel that will receive progress events.</returns>
    public static ChannelReader<CertificateProgressEvent> CreateProgressChannel(string sessionId)
    {
        var channel = Channel.CreateUnbounded<CertificateProgressEvent>();
        ActiveChannels.AddOrUpdate(sessionId, channel, (_, _) => channel);
        return channel.Reader;
    }

    /// <summary>
    /// Gets the progress channel for a specific session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The channel reader, or null if session not found.</returns>
    public static ChannelReader<CertificateProgressEvent>? GetProgressChannel(string sessionId)
    {
        return ActiveChannels.TryGetValue(sessionId, out var channel) ? channel.Reader : null;
    }

    /// <summary>
    /// Cleans up a progress channel after generation completes.
    /// </summary>
    /// <param name="sessionId">The session identifier to clean up.</param>
    public static void CleanupProgressChannel(string sessionId)
    {
        ActiveChannels.TryRemove(sessionId, out var channel);
        channel?.Writer.TryComplete();
    }

    /// <summary>
    /// Publishes a progress event to the active channel for the current session.
    /// </summary>
    /// <param name="progressEvent">The progress event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task PublishProgressAsync(CertificateProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        // This would be enhanced to use HttpContext to identify the current session
        // For now, this is a placeholder that will be called by the service
        return Task.CompletedTask;
    }
}