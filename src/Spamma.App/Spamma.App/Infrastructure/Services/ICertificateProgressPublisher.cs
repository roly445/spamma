namespace Spamma.App.Infrastructure.Services;

public interface ICertificateProgressPublisher
{
    Task PublishProgressAsync(CertificateProgressEvent progressEvent, CancellationToken cancellationToken = default);
}