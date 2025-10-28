using System.Security.Cryptography.X509Certificates;
using MaybeMonad;
using Microsoft.Extensions.Logging;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

/// <summary>
/// Service for managing SMTP TLS certificates.
/// </summary>
public class SmtpCertificateService(ILogger<SmtpCertificateService> logger)
{
    private readonly string _certificatePath = Path.Combine(AppContext.BaseDirectory, "certs");

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
            // Load the first certificate found
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            var certificate = new X509Certificate2(certFiles[0]);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
            logger.LogInformation("Found valid SMTP certificate at {Path}", certFiles[0]);
            return Maybe.From(certificate);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Certificate exists at {Path} but failed to load", certFiles[0]);
            return Maybe<X509Certificate2>.Nothing;
        }
    }
}



