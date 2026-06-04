using System.Security.Cryptography.X509Certificates;
using MaybeMonad;
using Microsoft.Extensions.Logging;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public class SmtpCertificateService(ILogger<SmtpCertificateService> logger)
{
    private const string CertificatePassword = "letmein";
    private readonly string _certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "certs");

    public Maybe<X509Certificate2> FindCertificate()
    {
        if (!Directory.Exists(this._certificatePath))
        {
            logger.LogDebug("Certificate directory not found at {Path}", this._certificatePath);
            return Maybe<X509Certificate2>.Nothing;
        }

        var certFiles = Directory.GetFiles(this._certificatePath, "*.pfx");

        if (certFiles.Length == 0)
        {
            logger.LogDebug("No .pfx certificates found in {Path}", this._certificatePath);
            return Maybe<X509Certificate2>.Nothing;
        }

        var certificates = certFiles
            .Select(this.TryLoadCertificate)
            .Where(c => c is not null)
            .Cast<LoadedCertificate>()
            .OrderByDescending(c => c.LastWriteTimeUtc)
            .ToList();

        if (certificates.Count == 0)
        {
            return Maybe<X509Certificate2>.Nothing;
        }

        var now = DateTime.UtcNow;
        var selected = certificates.FirstOrDefault(c =>
            c.Certificate.NotBefore.ToUniversalTime() <= now && c.Certificate.NotAfter.ToUniversalTime() > now);

        if (selected is null)
        {
            selected = certificates[0];
            logger.LogWarning(
                "No currently valid SMTP certificate found in {Path}; using newest loadable certificate {CertificatePath} which expires at {ExpiresAt:u}",
                this._certificatePath,
                selected.Path,
                selected.Certificate.NotAfter.ToUniversalTime());
        }

        foreach (var certificate in certificates.Where(c => c != selected))
        {
            certificate.Certificate.Dispose();
        }

        logger.LogInformation(
            "Found valid SMTP certificate at {Path} ({LoadMode}); expires at {ExpiresAt:u}",
            selected.Path,
            selected.LoadMode,
            selected.Certificate.NotAfter.ToUniversalTime());

        return Maybe.From(selected.Certificate);
    }

    private LoadedCertificate? TryLoadCertificate(string certificatePath)
    {
        try
        {
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            try
            {
                var certificate = new X509Certificate2(certificatePath, CertificatePassword);
                return new LoadedCertificate(certificatePath, File.GetLastWriteTimeUtc(certificatePath), "with password", certificate);
            }
            catch (Exception passwordEx)
            {
                logger.LogDebug(passwordEx, "Failed to load certificate at {Path} with password, trying without password", certificatePath);
                var certificate = new X509Certificate2(certificatePath);
                return new LoadedCertificate(certificatePath, File.GetLastWriteTimeUtc(certificatePath), "no password", certificate);
            }
#pragma warning restore SYSLIB0057 // Type or member is obsolete
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Certificate exists at {Path} but failed to load with or without password", certificatePath);
            return null;
        }
    }

    private sealed record LoadedCertificate(string Path, DateTime LastWriteTimeUtc, string LoadMode, X509Certificate2 Certificate);
}
