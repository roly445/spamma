using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Spamma.App.Client.Components.ChaosAddress;
using Spamma.App.Client.Infrastructure.Auth;
using Spamma.Modules.DomainManagement.Client.Application.Commands.DisableChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.EnableChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Subdomains;

public partial class ChaosAddresses(IQuerier querier,
    ICommander commander,
    IJSRuntime jsRuntime,
    AuthenticationStateProvider authenticationStateProvider)
{
    private bool isLoading = true;

    private CreateChaosAddress? createModal;
    private IReadOnlyList<ChaosAddressSummary>? chaosAddresses;

    private bool isInitialized;
    private string? errorMessage;

    private ChaosAddressSearchRequest searchRequest = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadChaosAddresses();
        isInitialized = true;
    }

    private async Task LoadChaosAddresses()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            var query = new GetChaosAddressesQuery();
            var result = await querier.Send(query);
            if (result.Status == QueryResultStatus.Succeeded && result.Data != null)
            {
                chaosAddresses = result.Data.Items;
            }
            else
            {
                errorMessage = "Failed to load chaos addresses.";
                chaosAddresses = new List<ChaosAddressSummary>();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading chaos addresses: {ex.Message}";
            chaosAddresses = new List<ChaosAddressSummary>();
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleSearch()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            var enabled = searchRequest.EnabledFilter switch
            {
                "enabled" => true,
                "disabled" => false,
                _ => (bool?)null
            };

            var query = new SearchChaosAddressesQuery(
                searchRequest.SearchTerm,
                null,
                enabled,
                1,
                50,
                searchRequest.SortBy,
                true);

            var result = await querier.Send(query);
            if (result.Status == QueryResultStatus.Succeeded && result.Data != null)
            {
                chaosAddresses = result.Data.Items;
            }
            else
            {
                errorMessage = "Failed to search chaos addresses.";
                chaosAddresses = new List<ChaosAddressSummary>();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error searching: {ex.Message}";
            chaosAddresses = new List<ChaosAddressSummary>();
        }
        finally
        {
            isLoading = false;
        }
    }

    private void OpenCreate() => createModal?.Open();

    private async Task HandleCreated()
    {
        await LoadChaosAddresses();
    }

    private async Task ToggleChaosAddress(Guid id, bool enable)
    {
        try
        {
            ICommand command = enable
                ? new EnableChaosAddressCommand(id, Guid.NewGuid())
                : (ICommand)new DisableChaosAddressCommand(id, Guid.NewGuid());

            var result = await commander.Send(command);
            if (result.Status == CommandResultStatus.Succeeded)
            {
                await LoadChaosAddresses();
            }
            else
            {
                errorMessage = "Failed to update chaos address.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error: {ex.Message}";
        }
    }

    private async Task DeleteChaosAddress(Guid _)
    {
        if (await jsRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this chaos address?"))
        {
            try
            {
                await LoadChaosAddresses();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error: {ex.Message}";
            }
        }
    }

    private class ChaosAddressSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string EnabledFilter { get; set; } = "";
        public string SortBy { get; set; } = "CreatedAt";
    }
}
