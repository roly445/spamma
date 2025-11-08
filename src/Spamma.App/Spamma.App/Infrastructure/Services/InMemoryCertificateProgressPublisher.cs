using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Spamma.App.Infrastructure.Services;

public sealed class InMemoryCertificateProgressPublisher : ICertificateProgressPublisher
{
    private static readonly ConcurrentDictionary<string, Channel<CertificateProgressEvent>> ActiveChannels =
        new();
    public static ChannelReader<CertificateProgressEvent> CreateProgressChannel(string sessionId)
    {
        var channel = Channel.CreateUnbounded<CertificateProgressEvent>();
        ActiveChannels.AddOrUpdate(sessionId, channel, (_, _) => channel);
        return channel.Reader;
    }

    public static ChannelReader<CertificateProgressEvent>? GetProgressChannel(string sessionId)
    {
        return ActiveChannels.TryGetValue(sessionId, out var channel) ? channel.Reader : null;
    }

    public static void CleanupProgressChannel(string sessionId)
    {
        ActiveChannels.TryRemove(sessionId, out var channel);
        channel?.Writer.TryComplete();
    }

    public Task PublishProgressAsync(CertificateProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        // This would be enhanced to use HttpContext to identify the current session
        // For now, this is a placeholder that will be called by the service
        return Task.CompletedTask;
    }
}