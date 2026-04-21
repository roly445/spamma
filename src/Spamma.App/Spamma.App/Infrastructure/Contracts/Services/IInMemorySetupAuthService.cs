namespace Spamma.App.Infrastructure.Contracts.Services;

public interface IInMemorySetupAuthService
{
    bool IsSetupModeEnabled { get; }

    string GetCurrentSetupPassword();

    bool ValidatePassword(string password, string ipAddress, string userAgent);

    void DisableSetupMode();

    void EnableMaintenanceMode(string reason);

    string GetSetupReason();
}