namespace Spamma.App.Infrastructure.Services;

/// <summary>
/// Interface for publishing certificate generation progress events.
/// </summary>
public interface ICertificateProgressPublisher
{
    /// <summary>
    /// Publishes a progress event asynchronously.
    /// </summary>
    /// <param name="progressEvent">The progress event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishProgressAsync(CertificateProgressEvent progressEvent, CancellationToken cancellationToken = default);
}
