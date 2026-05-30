using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

internal sealed class SelfSignedCertificateService : ISelfSignedCertificateService
{
    private const string CertificatePassword = "letmein";

    public byte[] GenerateSelfSignedCertificate(string domain)
    {
        using var rsa = RSA.Create(2048);

        var req = new CertificateRequest($"CN={domain}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName(domain);
        req.CertificateExtensions.Add(sanBuilder.Build());

        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
        return cert.Export(X509ContentType.Pfx, CertificatePassword);
    }
}
