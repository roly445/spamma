using System.Globalization;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.Modules.Common.Infrastructure.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.App.Infrastructure.Services;

public sealed class CertificateRenewalBackgroundService(
    ILogger<CertificateRenewalBackgroundService> logger,
    IServiceProvider serviceProvider,
    IHostEnvironment hostEnvironment)
    : BackgroundService
{
    private const int RenewalThresholdDays = 30;
    private const int CertificateRetentionCount = 3;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeOnly RenewalTime = new TimeOnly(2, 0, 0);

    private readonly string _certificatesPath = Path.Combine(hostEnvironment.ContentRootPath, "certs");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Certificate renewal background service started");

        try
        {
            // Ensure certificate directory exists
            if (!Directory.Exists(this._certificatesPath))
            {
                Directory.CreateDirectory(this._certificatesPath);
                logger.LogInformation("Created certificate directory: {Path}", this._certificatesPath);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = TimeOnly.FromDateTime(DateTime.UtcNow);
                    var timeUntilRenewal = CertificateRenewalBackgroundService.CalculateTimeUntilRenewal(now);

                    logger.LogDebug("Next renewal check in {TimeSpan}", timeUntilRenewal);
                    await Task.Delay(timeUntilRenewal, stoppingToken);

                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    await this.PerformCertificateRenewalAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during certificate renewal check. The renewal process will retry on the next scheduled check");
                }
            }
        }
        finally
        {
            logger.LogInformation("Certificate renewal background service stopped");
        }
    }

    private static TimeSpan CalculateTimeUntilRenewal(TimeOnly currentTime)
    {
        var timeUntilRenewal = RenewalTime - currentTime;

        if (timeUntilRenewal.TotalSeconds <= 0)
        {
            // Renewal time already passed today, schedule for tomorrow
            timeUntilRenewal = timeUntilRenewal.Add(TimeSpan.FromHours(24));
        }

        // Cap at CheckInterval to perform checks periodically
        return timeUntilRenewal > CheckInterval ? CheckInterval : timeUntilRenewal;
    }

    private static string GenerateCertificateFileName()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss", CultureInfo.InvariantCulture);
        return $"certificate_{timestamp}.pfx";
    }

    private async Task PerformCertificateRenewalAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting certificate renewal check at {Time:u}", DateTime.UtcNow);

        using var scope = serviceProvider.CreateScope();

        try
        {
            var configService = scope.ServiceProvider.GetRequiredService<IAppConfigurationService>();
            var certService = scope.ServiceProvider.GetRequiredService<ICertesLetsEncryptService>();
            var challengeResponder = scope.ServiceProvider.GetRequiredService<IAcmeChallengeResponder>();

            // Get application configuration
            var appSettings = await configService.GetApplicationSettingsAsync();
            if (appSettings is null || string.IsNullOrWhiteSpace(appSettings.MailServerHostname))
            {
                logger.LogWarning("Application settings not configured, skipping renewal");
                return;
            }

            var emailSettings = await configService.GetEmailSettingsAsync();
            if (emailSettings is null || string.IsNullOrWhiteSpace(emailSettings.FromEmail))
            {
                logger.LogWarning("Email settings not configured, skipping renewal");
                return;
            }

            // Check current certificate expiration
            var currentCertFile = this.FindLatestCertificate();
            if (currentCertFile is not null && !this.ShouldRenew(currentCertFile))
            {
                logger.LogDebug("Certificate still valid, no renewal needed");
                return;
            }

            logger.LogInformation("Certificate needs renewal, starting generation");

            // Use mail server hostname as primary domain for certificate
            var domain = appSettings.MailServerHostname;
            var email = emailSettings.FromEmail;

            // Generate new certificate using production Let's Encrypt
            var result = await certService.GenerateCertificateAsync(
                domain,
                email,
                useStaging: false,
                challengeResponder,
                progressPublisher: null,
                cancellationToken);

            if (!result.IsSuccess)
            {
                logger.LogError("Certificate generation failed: {Error}", result.Error);
                return;
            }

            // Save the new certificate
            var certFileName = CertificateRenewalBackgroundService.GenerateCertificateFileName();
            var certFilePath = Path.Combine(this._certificatesPath, certFileName);

            await File.WriteAllBytesAsync(certFilePath, result.Value, cancellationToken);
            logger.LogInformation("Certificate saved to {Path}", certFilePath);

            // Clean up old certificates, keep only last 3
            this.CleanupOldCertificates();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during certificate renewal process");
        }
    }

    private FileInfo? FindLatestCertificate()
    {
        try
        {
            var directory = new DirectoryInfo(this._certificatesPath);
            if (!directory.Exists)
            {
                return null;
            }

            var certFiles = directory.GetFiles("certificate_*.pfx")
                .OrderByDescending(f => f.CreationTimeUtc)
                .FirstOrDefault();

            return certFiles;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding latest certificate");
            return null;
        }
    }

    private bool ShouldRenew(FileInfo certFile)
    {
        try
        {
            // Extract certificate validity based on filename or modification time
            // For now, use file modification time as proxy for cert generation time
            // In production, parse the actual PFX file to check expiration
            var certAge = DateTime.UtcNow - certFile.LastWriteTimeUtc;
            var shouldRenew = certAge.TotalDays > (365 - RenewalThresholdDays);

            logger.LogDebug("Certificate age: {Days:F1} days, should renew: {ShouldRenew}", certAge.TotalDays, shouldRenew);
            return shouldRenew;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking certificate expiration");
            return true; // Renew if we can't determine expiration
        }
    }

    private void CleanupOldCertificates()
    {
        try
        {
            var directory = new DirectoryInfo(this._certificatesPath);
            if (!directory.Exists)
            {
                return;
            }

            var certFiles = directory.GetFiles("certificate_*.pfx")
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            if (certFiles.Count <= CertificateRetentionCount)
            {
                return;
            }

            // Delete old certificates beyond retention count
            var filesToDelete = certFiles.Skip(CertificateRetentionCount);
            foreach (var file in filesToDelete)
            {
                try
                {
                    file.Delete();
                    logger.LogInformation("Deleted old certificate: {FileName}", file.Name);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete old certificate: {FileName}", file.Name);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up old certificates");
        }
    }
}