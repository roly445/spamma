using Microsoft.Extensions.Options;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.App.Infrastructure.Contracts.Settings;

namespace Spamma.App.Infrastructure.Services
{
    public class SetupDetectionService(IOptions<SetupSettings> setupSettings, ILogger<SetupDetectionService> logger) : ISetupDetectionService
    {
        private readonly SetupSettings settings = setupSettings.Value;

        private bool? _cachedSetupMode;
        private string? _cachedReason;

        public Task<bool> ShouldEnableSetupModeAsync()
        {
            if (this._cachedSetupMode.HasValue)
            {
                return Task.FromResult(this._cachedSetupMode.Value);
            }

            try
            {
                this._cachedSetupMode = !this.settings.Completed.HasValue;

                return Task.FromResult(this._cachedSetupMode.Value);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not check setup completion status from database, assuming setup needed");
                this._cachedSetupMode = true;
                return Task.FromResult(true);
            }
        }

        public async Task<string> GetSetupReasonAsync()
        {
            if (this._cachedReason != null)
            {
                return this._cachedReason;
            }

            var setupNeeded = await this.ShouldEnableSetupModeAsync();
            this._cachedReason = setupNeeded ? "Initial setup required" : "Setup already completed";
            return this._cachedReason;
        }

        public bool ShouldEnableSetupMode()
        {
            return this.ShouldEnableSetupModeAsync().GetAwaiter().GetResult();
        }

        public string GetSetupReason()
        {
            return this.GetSetupReasonAsync().GetAwaiter().GetResult();
        }
    }
}