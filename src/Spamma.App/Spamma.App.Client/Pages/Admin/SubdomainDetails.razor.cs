using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Spamma.App.Client.Components.UserControls;
using Spamma.App.Client.Components.UserControls.Subdomain;
using Spamma.App.Client.Infrastructure.Contracts;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the subdomain details page.
/// </summary>
public partial class SubdomainDetails(ICommander commander, IQuerier querier, NavigationManager navigation, IOptions<Settings> settings) : ComponentBase
{
    private SuspendSubdomain? suspendSubdomain;
    private UnsuspendSubdomain? unsuspendSubdomain;
    private EditSubdomain? editSubdomain;

    private int _activeTab;
    private AddUser? addModerator;
    private AddUser? _addViewer;
    private UserAssignment? moderatorAssignments;
    private UserAssignment? _viewerAssignments;
    private GetDetailedSubdomainByIdQueryResult? subdomain;

    private bool isLoading = true;
    private bool isCheckingMx;
    private Settings settings = settings.Value;

    [Parameter]
    public Guid Id { get; set; }

    [Parameter]
    public Guid DomainId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await this.LoadSubdomainDetails();
    }

    private static string GetStatusText(SubdomainStatus status) => status switch
    {
        SubdomainStatus.Active => "Active",
        SubdomainStatus.Inactive => "Inactive",
        SubdomainStatus.Suspended => "Suspended",
        _ => "Unknown",
    };

    private void OpenSuspendSubdomain()
    {
        if (this.subdomain == null || this.suspendSubdomain == null)
        {
            return;
        }

        this.suspendSubdomain.Open(new SuspendSubdomain.DataModel(this.subdomain.SubdomainId, this.subdomain.FullName));
    }

    private Task HandleSubdomainSuspended()
    {
        return this.LoadSubdomainDetails();
    }

    private void OpenUnsuspendSubdomain()
    {
        if (this.subdomain == null || this.unsuspendSubdomain == null)
        {
            return;
        }

        this.unsuspendSubdomain.Open(new UnsuspendSubdomain.DataModel(
            this.subdomain.SubdomainId, this.subdomain.FullName, this.subdomain.CreatedAt));
    }

    private Task HandleSubdomainUnsuspended()
    {
        return this.LoadSubdomainDetails();
    }

    private void SetActiveTab(int tabIndex)
    {
        this._activeTab = tabIndex;
    }

    private Task HandleSubdomainUpdated()
    {
        return this.LoadSubdomainDetails();
    }

    private void OpenEditSubdomain()
    {
        if (this.subdomain == null || this.editSubdomain == null)
        {
            return;
        }

        this.editSubdomain.Open(new EditSubdomain.DataModel(
            this.subdomain.SubdomainId,
            this.subdomain.Description,
            this.subdomain.Name,
            this.subdomain.DomainName,
            this.subdomain.CreatedAt));
    }

    private void OpenAddModerator()
    {
        this.addModerator?.Open();
    }

    private Task HandleModeratorAdded()
    {
        return this.moderatorAssignments == null ? Task.CompletedTask : this.moderatorAssignments.Reload();
    }

    private void OpenAddViewer()
    {
        this._addViewer?.Open();
    }

    private Task HandleViewerAdded()
    {
        return this._viewerAssignments == null ? Task.CompletedTask : this._viewerAssignments.Reload();
    }

    private async Task LoadSubdomainDetails()
    {
        this.isLoading = true;
        this.StateHasChanged();

        try
        {
            var result = await querier.Send(new GetDetailedSubdomainByIdQuery(this.Id));

            if (result.Status == QueryResultStatus.Succeeded)
            {
                this.subdomain = result.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load subdomain details: {ex.Message}");
            this.subdomain = null;
        }
        finally
        {
            this.isLoading = false;
            this.StateHasChanged();
        }
    }

    private async Task CheckMxRecords()
    {
        if (this.subdomain == null)
        {
            return;
        }

        this.isCheckingMx = true;
        this.StateHasChanged();

        var result = await commander.Send(new CheckMxRecordCommand(this.subdomain.SubdomainId));

        if (result.Status == CommandResultStatus.Succeeded)
        {
            // Update local MX check result
            this.subdomain = this.subdomain with
            {
                MxStatus = result.Data.MxStatus,
                MxCheckedAt = result.Data.WhenChecked,
            };
        }

        this.isCheckingMx = false;
        this.StateHasChanged();
    }

    private void GoBack() => navigation.NavigateTo("/admin/subdomains");

    private void NavigateToParentDomain()
    {
        if (this.subdomain?.DomainId != null)
        {
            navigation.NavigateTo($"/admin/domains/{this.subdomain.DomainId}");
        }
    }
}