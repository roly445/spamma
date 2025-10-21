namespace Spamma.App.Infrastructure.Contracts.Services;

public interface ISetupDetectionService
{
    bool ShouldEnableSetupMode();

    string GetSetupReason();
}