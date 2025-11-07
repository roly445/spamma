using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spamma.App.Client.Components.UserControls.Domain;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the domains management page.
/// </summary>
public partial class Domains(
    ICommander commander,
    IQuerier querier,
    NavigationManager navigationManager,
    IDomainValidationService domainValidationService,
    IJSRuntime jsRuntime, INotificationService notificationService) : ComponentBase
{
    private DomainSearchRequest searchRequest = new();

    private SearchDomainsQueryResult? _pagedResult;
    private bool isLoading = true;
    private EditDomain? _editDomain;
    private SuspendDomain? _suspendDomain;
    private UnsuspendDomain? _unsuspendDomain;
    private VerifyDomain? _verifyDomain;

    public enum VerificationStatus
    {
        All,
        Verified,
        Unverified,
    }

    protected override async Task OnInitializedAsync()
    {
        await this.LoadDomains();
    }

    private static string GetStatusClasses(DomainStatus status)
    {
        return status switch
        {
            DomainStatus.Active => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-green-100 text-green-800",
            DomainStatus.Pending => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
            DomainStatus.Suspended => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-red-100 text-red-800",
            _ => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
        };
    }

    private static string GetStatusText(DomainStatus status)
    {
        return status switch
        {
            DomainStatus.Active => "Active",
            DomainStatus.Pending => "Pending",
            DomainStatus.Suspended => "Suspended",
            _ => "Unknown",
        };
    }

    private async Task HandleSearch()
    {
        this.searchRequest.Page = 1;
        await this.LoadDomains();
    }

    private async Task GoToPage(int page)
    {
        this.searchRequest.Page = page;
        await this.LoadDomains();
    }

    private async Task LoadDomains()
    {
        this.isLoading = true;
        this.StateHasChanged();

        var result = await querier.Send(new SearchDomainsQuery(
            this.searchRequest.SearchTerm,
            this.searchRequest.Status,
            this.searchRequest.IsVerified == VerificationStatus.All ? null : this.searchRequest.IsVerified == VerificationStatus.Verified,
            this.searchRequest.Page,
            this.searchRequest.PageSize,
            this.searchRequest.SortBy,
            this.searchRequest.SortDescending));
        if (result.Status == QueryResultStatus.Succeeded)
        {
            this._pagedResult = result.Data;
        }
        else
        {
            this._pagedResult = new SearchDomainsQueryResult(new List<SearchDomainsQueryResult.DomainSummary>(), 0, 0, 0, 0);
        }

        this.isLoading = false;
        this.StateHasChanged();
    }

    private async Task CopyToClipboard(string text = "")
    {
        await jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }

    private void NavigateToDomainDetails(Guid id)
    {
        navigationManager.NavigateTo($"/admin/domains/{id}");
    }

    private void OpenEditDomain(SearchDomainsQueryResult.DomainSummary domain)
    {
        if (this._editDomain == null)
        {
            return;
        }

        this._editDomain.Open(new EditDomain.DataModel(
            domain.DomainId,
            domain.DomainName,
            domain.PrimaryContact,
            domain.Description,
            domain.CreatedAt,
            domain.IsVerified,
            domain.VerifiedAt,
            domain.SubdomainCount,
            domain.AssignedUserCount));
    }

    private Task HandleEditSaved()
    {
        return this.LoadDomains();
    }

    private void OpenSuspendDomain(SearchDomainsQueryResult.DomainSummary domain)
    {
        this._suspendDomain!.Open(new SuspendDomain.DataModel(domain.DomainId, domain.DomainName));
    }

    private Task HandleDomainSuspended()
    {
        return this.LoadDomains();
    }

    private void OpenUnsuspendDomain(SearchDomainsQueryResult.DomainSummary domain)
    {
        if (this._unsuspendDomain == null)
        {
            return;
        }

        this._unsuspendDomain.Open(new UnsuspendDomain.DataModel(domain.DomainId, domain.DomainName));
    }

    private Task HandleDomainUnsuspended()
    {
        if (this._unsuspendDomain == null)
        {
            return Task.CompletedTask;
        }

        this._unsuspendDomain.Close();
        return this.LoadDomains();
    }

    private void OpenVerifyDomain(SearchDomainsQueryResult.DomainSummary domain)
    {
        if (this._verifyDomain == null)
        {
            return;
        }

        this._verifyDomain.Open(new VerifyDomain.DataModel(
            domain.DomainId,
            domain.DomainName,
            domain.VerificationToken));
    }

    private Task HandleVerified()
    {
        return this.LoadDomains();
    }

    private sealed class DomainSearchRequest
    {
        public string? SearchTerm { get; set; }

        public DomainStatus? Status { get; set; }

        public VerificationStatus IsVerified { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string SortBy { get; set; } = "CreatedAt";

        public bool SortDescending { get; set; } = true;
    }
}