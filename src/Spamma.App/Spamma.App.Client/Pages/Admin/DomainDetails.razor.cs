using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.App.Client.Pages.Admin;

public partial class DomainDetails(ICommander commander, IQuerier querier, NavigationManager navigation, IJSRuntime jsRuntime) : ComponentBase
{
    private GetDetailedDomainByIdQueryResult? domain;
    private bool _isLoading = true;
    private bool _isCheckingVerification;
    private int _activeTab;

    [Parameter]
    public Guid Id { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await this.LoadAsync();
    }

    private static string StatusBadge(DomainStatus status) => status switch
    {
        DomainStatus.Active => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-green-100 text-green-800",
        DomainStatus.Pending => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
        DomainStatus.Suspended => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-red-100 text-red-800",
        _ => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
    };

    private async Task LoadAsync()
    {
        this._isLoading = true;
        this.StateHasChanged();
        try
        {
            var result = await querier.Send(new GetDetailedDomainByIdQuery(this.Id));
            if (result.Status == QueryResultStatus.Succeeded)
            {
                this.domain = result.Data;
            }
        }
        finally
        {
            this._isLoading = false;
            this.StateHasChanged();
        }
    }

    private void GoBack()
    {
        navigation.NavigateTo("/admin/domains");
    }

    private async Task CopyToClipboard(string text)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
        catch
        {
            // no-op fallback
        }
    }

    private void SetActiveTab(int tabIndex)
    {
        this._activeTab = tabIndex;
    }
}