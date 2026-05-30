namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public interface ISelfSignedCertificateService
{
    byte[] GenerateSelfSignedCertificate(string domain);
}
