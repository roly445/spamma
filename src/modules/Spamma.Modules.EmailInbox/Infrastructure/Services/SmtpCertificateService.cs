using System.Security.Cryptography.X509Certificates;
using MaybeMonad;
using Microsoft.Extensions.Logging;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

/// <summary>
/// Service for managing SMTP TLS certificates.
/// </summary>
public class SmtpCertificateService(ILogger<SmtpCertificateService> logger)
{
    private const string CertificatePassword = "letmein";
    private readonly string _certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "certs");

    /// <summary>
    /// Attempts to find and load a certificate from the certs directory.
    /// </summary>
    /// <returns>The first valid certificate found, or Nothing if none exist.</returns>
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

        try
        {
            // Try loading the first certificate with password (generated certs)
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            try
            {
                var certificate = new X509Certificate2(certFiles[0], CertificatePassword);
                logger.LogInformation("Found valid SMTP certificate at {Path} (with password)", certFiles[0]);
                return Maybe.From(certificate);
            }
            catch (Exception passwordEx)
            {
                // Fall back to loading without password (manually added certs)
                logger.LogDebug(passwordEx, "Failed to load certificate with password, trying without password");
                var certificate = new X509Certificate2(certFiles[0]);
                logger.LogInformation("Found valid SMTP certificate at {Path} (no password)", certFiles[0]);
                return Maybe.From(certificate);
            }
#pragma warning restore SYSLIB0057 // Type or member is obsolete
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Certificate exists at {Path} but failed to load with or without password", certFiles[0]);
            return Maybe<X509Certificate2>.Nothing;
        }
    }
}


