using Microsoft.AspNetCore.Components;
using Spamma.App.Components.Layout;
using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Components.Pages.Setup;

public partial class Keys(IAppConfigurationService appConfigurationService)
{
    private string? successMessage;
    private bool hasExistingKeys;

    [SupplyParameterFromForm(FormName = "KeysForm")]
    public KeysModel? Model { get; set; }

    [CascadingParameter]
    public SetupLayout Layout { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        this.Layout.CurrentStep = "1";
        this.Layout.CompletedSteps.Add("0");
        await this.CheckForExistingKeys();
        if (this.Model == null)
        {
            this.Model = new KeysModel();
        }
    }

    private async Task CheckForExistingKeys()
    {
        try
        {
            var existingKeys = await appConfigurationService.GetKeySettingsAsync();
            this.hasExistingKeys = !string.IsNullOrEmpty(existingKeys.SigningKey);
        }
        catch
        {
            // If we can't check for keys, assume they don't exist
            this.hasExistingKeys = false;
        }
    }

    private async Task HandleSaveKeys()
    {
        if (this.Model == null)
        {
            return;
        }

        await appConfigurationService.SaveKeysAsync(new IAppConfigurationService.KeySettings
        {
            SigningKey = this.Model.SigningKey!,
        });

        this.successMessage = "Keys saved successfully!";
    }

    public class KeysModel
    {
        public string? SigningKey { get; set; }
    }
}