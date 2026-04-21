using System.Security.Cryptography;
using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Infrastructure.Services;

public class InMemorySetupAuthService : IInMemorySetupAuthService
{
    private readonly string setupReason;
    private readonly ILogger<InMemorySetupAuthService> _logger;
    private string _setupPassword;
    private bool _setupModeEnabled;

    public InMemorySetupAuthService(
        ILogger<InMemorySetupAuthService> logger,
        ISetupDetectionService setupDetection)
    {
        this._logger = logger;

        // Check if setup mode should be enabled
        this._setupModeEnabled = setupDetection.ShouldEnableSetupMode();
        this.setupReason = setupDetection.GetSetupReason();

        if (this._setupModeEnabled)
        {
            this._setupPassword = this.ResolveSetupPassword();
            this.LogSetupModeActive("SETUP MODE ENABLED", this.setupReason);
        }
        else
        {
            this._setupPassword = string.Empty;
            this._logger.LogInformation("Spamma is properly configured - setup mode disabled");
        }
    }

    public bool IsSetupModeEnabled => this._setupModeEnabled;

    public string GetCurrentSetupPassword() => this._setupPassword;

    public string GetSetupReason() => this.setupReason;

    public bool ValidatePassword(string password, string ipAddress, string userAgent)
    {
        if (!this._setupModeEnabled)
        {
            this._logger.LogWarning("Setup authentication attempted but setup mode is disabled from {IpAddress}", ipAddress);
            return false;
        }

        var isValid = !string.IsNullOrEmpty(password) && password == this._setupPassword;

        this._logger.LogWarning(
            "Setup login attempt: IP={IpAddress}, UserAgent={UserAgent}, Password={Password}, Valid={IsValid}",
            ipAddress, userAgent?.Substring(0, Math.Min(userAgent?.Length ?? 0, 100)), password, isValid);

        if (isValid)
        {
            this._logger.LogInformation("Successful setup authentication from {IpAddress}", ipAddress);
        }
        else
        {
            this._logger.LogWarning("Failed setup authentication from {IpAddress} - Incorrect password: '{Password}'", ipAddress, password);
        }

        return isValid;
    }

    public void DisableSetupMode()
    {
        this._setupModeEnabled = false;
        this._logger.LogInformation("Setup mode disabled - setup wizard completed");
        this._logger.LogInformation("Setup password '{SetupPassword}' is no longer valid", this._setupPassword);

        // Optional: Create setup completion marker
        this.CreateSetupCompletionMarker();
    }

    public void EnableMaintenanceMode(string reason)
    {
        this._setupModeEnabled = true;
        this._setupPassword = this.ResolveSetupPassword();
        this.LogSetupModeActive("MAINTENANCE MODE ENABLED", reason);
    }

    private static string GenerateSetupPassword()
    {
        var adjectives = new[] { "Quick", "Blue", "Smart", "Cool", "Fast", "Bright", "Strong", "Swift" };
        var nouns = new[] { "Tiger", "Eagle", "Wolf", "Bear", "Lion", "Hawk", "Fox", "Shark" };

        var adjIndex = RandomNumberGenerator.GetInt32(adjectives.Length);
        var nounIndex = RandomNumberGenerator.GetInt32(nouns.Length);
        var number = RandomNumberGenerator.GetInt32(100, 999);

        return $"{adjectives[adjIndex]}{nouns[nounIndex]}{number}";
    }

    private string ResolveSetupPassword()
    {
        var envPassword = Environment.GetEnvironmentVariable("SPAMMA_SETUP_PASSWORD");
        if (!string.IsNullOrWhiteSpace(envPassword))
        {
            this._logger.LogCritical("Using SPAMMA_SETUP_PASSWORD environment variable for setup authentication");
            return envPassword;
        }

        return GenerateSetupPassword();
    }

    private void LogSetupModeActive(string heading, string reason)
    {
        this._logger.LogCritical("=".PadRight(80, '='));
        this._logger.LogCritical("SPAMMA {Heading}", heading);
        this._logger.LogCritical("Reason: {Reason}", reason);
        this._logger.LogCritical("SETUP PASSWORD: {SetupPassword}", this._setupPassword);
        this._logger.LogCritical("Access setup wizard at: /setup-login");
        this._logger.LogCritical("Recovery: set SPAMMA_SETUP_PASSWORD env var and restart to override");
        this._logger.LogCritical("=".PadRight(80, '='));
    }

    private void CreateSetupCompletionMarker()
    {
        try
        {
            var setupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Spamma");

            Directory.CreateDirectory(setupDir);

            var markerFile = Path.Combine(setupDir, ".setup-complete");
            File.WriteAllText(markerFile, $"Setup completed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

            this._logger.LogInformation("Created setup completion marker at: {Path}", markerFile);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to create setup completion marker");
        }
    }
}