using System.Globalization;
using Microsoft.Extensions.Configuration;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.Modules.Common.Infrastructure.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.App.Infrastructure.Services;

/// <summary>
/// Background service that checks certificate expiration daily and renews if needed.
/// Runs at 2 AM UTC each day and renews certificates with less than 30 days remaining.
/// </summary>
public sealed class CertificateRenewalBackgroundService : BackgroundService
{
    private const int RenewalThresholdDays = 30;
    private const int CertificateRetentionCount = 3;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeOnly RenewalTime = new TimeOnly(2, 0, 0);

    private readonly ILogger<CertificateRenewalBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _certificatesPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateRenewalBackgroundService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    /// <param name="hostEnvironment">Host environment for content root path.</param>
    public CertificateRenewalBackgroundService(
        ILogger<CertificateRenewalBackgroundService> logger,
        IServiceProvider serviceProvider,
        IHostEnvironment hostEnvironment)
    {
        this._logger = logger;
        this._serviceProvider = serviceProvider;
        this._certificatesPath = Path.Combine(hostEnvironment.ContentRootPath, "certs");
    }

    /// <summary>
    /// Executes the background service.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for stopping the service.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("Certificate renewal background service started");

        try
        {
            // Ensure certificate directory exists
            if (!Directory.Exists(this._certificatesPath))
            {
                Directory.CreateDirectory(this._certificatesPath);
                this._logger.LogInformation("Created certificate directory: {Path}", this._certificatesPath);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = TimeOnly.FromDateTime(DateTime.UtcNow);
                    var timeUntilRenewal = CertificateRenewalBackgroundService.CalculateTimeUntilRenewal(now);

                    this._logger.LogDebug("Next renewal check in {TimeSpan}", timeUntilRenewal);
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
                    this._logger.LogError(ex, "Error during certificate renewal check. The renewal process will retry on the next scheduled check");
                }
            }
        }
        finally
        {
            this._logger.LogInformation("Certificate renewal background service stopped");
        }
    }

    /// <summary>
    /// Calculates the time until the next renewal check (2 AM UTC).
    /// </summary>
    /// <param name="currentTime">Current time.</param>
    /// <returns>TimeSpan until next renewal check.</returns>
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

    /// <summary>
    /// Generates a timestamped certificate filename.
    /// </summary>
    /// <returns>Certificate filename with timestamp.</returns>
    private static string GenerateCertificateFileName()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss", CultureInfo.InvariantCulture);
        return $"certificate_{timestamp}.pfx";
    }

    /// <summary>
    /// Performs the certificate renewal check and renewal if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task PerformCertificateRenewalAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Starting certificate renewal check at {Time:u}", DateTime.UtcNow);

        using var scope = this._serviceProvider.CreateScope();

        try
        {
            var configService = scope.ServiceProvider.GetRequiredService<IAppConfigurationService>();
            var certService = scope.ServiceProvider.GetRequiredService<ICertesLetsEncryptService>();
            var challengeResponder = scope.ServiceProvider.GetRequiredService<IAcmeChallengeResponder>();

            // Get application configuration
            var appSettings = await configService.GetApplicationSettingsAsync();
            if (appSettings is null || string.IsNullOrWhiteSpace(appSettings.MailServerHostname))
            {
                this._logger.LogWarning("Application settings not configured, skipping renewal");
                return;
            }

            var emailSettings = await configService.GetEmailSettingsAsync();
            if (emailSettings is null || string.IsNullOrWhiteSpace(emailSettings.FromEmail))
            {
                this._logger.LogWarning("Email settings not configured, skipping renewal");
                return;
            }

            // Check current certificate expiration
            var currentCertFile = this.FindLatestCertificate();
            if (currentCertFile is not null && !this.ShouldRenew(currentCertFile))
            {
                this._logger.LogDebug("Certificate still valid, no renewal needed");
                return;
            }

            this._logger.LogInformation("Certificate needs renewal, starting generation");

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
                this._logger.LogError("Certificate generation failed: {Error}", result.Error);
                return;
            }

            // Save the new certificate
            var certFileName = CertificateRenewalBackgroundService.GenerateCertificateFileName();
            var certFilePath = Path.Combine(this._certificatesPath, certFileName);

            await File.WriteAllBytesAsync(certFilePath, result.Value, cancellationToken);
            this._logger.LogInformation("Certificate saved to {Path}", certFilePath);

            // Clean up old certificates, keep only last 3
            this.CleanupOldCertificates();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error during certificate renewal process");
        }
    }

    /// <summary>
    /// Finds the latest certificate file in the certificates directory.
    /// </summary>
    /// <returns>FileInfo of the latest certificate, or null if none found.</returns>
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
            this._logger.LogError(ex, "Error finding latest certificate");
            return null;
        }
    }

    /// <summary>
    /// Determines if the certificate should be renewed based on expiration date.
    /// </summary>
    /// <param name="certFile">Certificate file to check.</param>
    /// <returns>True if certificate should be renewed, false otherwise.</returns>
    private bool ShouldRenew(FileInfo certFile)
    {
        try
        {
            // Extract certificate validity based on filename or modification time
            // For now, use file modification time as proxy for cert generation time
            // In production, parse the actual PFX file to check expiration
            var certAge = DateTime.UtcNow - certFile.LastWriteTimeUtc;
            var shouldRenew = certAge.TotalDays > (365 - RenewalThresholdDays);

            this._logger.LogDebug("Certificate age: {Days:F1} days, should renew: {ShouldRenew}", certAge.TotalDays, shouldRenew);
            return shouldRenew;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error checking certificate expiration");
            return true; // Renew if we can't determine expiration
        }
    }

    /// <summary>
    /// Removes old certificate files, keeping only the most recent ones.
    /// </summary>
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
                    this._logger.LogInformation("Deleted old certificate: {FileName}", file.Name);
                }
                catch (Exception ex)
                {
                    this._logger.LogWarning(ex, "Failed to delete old certificate: {FileName}", file.Name);
                }
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error cleaning up old certificates");
        }
    }
}
