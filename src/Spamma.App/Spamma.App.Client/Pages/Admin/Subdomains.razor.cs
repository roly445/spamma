using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Components.UserControls;
using Spamma.App.Client.Shared;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Code-behind for the Subdomains page.
/// </summary>
public partial class Subdomains(IQuerier querier, NavigationManager navigation) : ComponentBase
{
    private SubdomainSearchRequest searchRequest = new();

    private SearchSubdomainsQueryResult? pagedResult;
    private IReadOnlyList<DomainOption>? availableDomains;
    private bool isLoading = true;
    private AddSubdomain? addSubdomain;

    [Parameter]
    public Guid? DomainId { get; set; }

    [Parameter]
    public bool IsUsedAsComponent { get; set; }

    public Task Reload()
    {
        return this.LoadSubdomains();
    }

    protected override async Task OnInitializedAsync()
    {
        await this.LoadAvailableDomains();
        await this.LoadSubdomains();
    }

    private async Task HandleSearch()
    {
        this.searchRequest.Page = 1; // Reset to first page
        await this.LoadSubdomains();
    }

    private async Task GoToPage(int page)
    {
        this.searchRequest.Page = page;
        await this.LoadSubdomains();
    }

    private async Task LoadAvailableDomains()
    {
        if (this.DomainId == null)
        {
            var result = await querier.Send(new SearchDomainsQuery(SearchTerm: null, Status: DomainStatus.Active,
                IsVerified: true, Page: 1, PageSize: 100, SortBy: "domainname", SortDescending: false));
            if (result.Status == QueryResultStatus.Succeeded)
            {
                this.availableDomains = result.Data.Items.Select(d => new DomainOption(d.DomainId, d.DomainName)).ToList();
            }
            else
            {
                this.availableDomains = new List<DomainOption>();
            }
        }
        else
        {
            var result = await querier.Send(new GetDetailedDomainByIdQuery(this.DomainId.Value));
            if (result.Status == QueryResultStatus.Succeeded)
            {
                var domain = result.Data;
                this.availableDomains = new List<DomainOption> { new DomainOption(domain.DomainId, domain.DomainName) };
            }
            else
            {
                this.availableDomains = new List<DomainOption>();
            }
        }
    }

    private async Task LoadSubdomains()
    {
        this.isLoading = true;
        this.StateHasChanged();

        try
        {
            var queryResult = await querier.Send(new SearchSubdomainsQuery(
                SearchTerm: this.searchRequest.SearchTerm,
                ParentDomainId: this.DomainId ?? this.searchRequest.ParentDomainId,
                Status: this.searchRequest.Status,
                Page: this.searchRequest.Page,
                PageSize: this.searchRequest.PageSize,
                SortBy: this.searchRequest.SortBy,
                SortDescending: this.searchRequest.SortDescending));
            if (queryResult.Status == QueryResultStatus.Succeeded)
            {
                this.pagedResult = queryResult.Data;
            }
            else
            {
                this.pagedResult = new SearchSubdomainsQueryResult(new List<SearchSubdomainsQueryResult.SubdomainSummary>(), 0, 0, 0, 0);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading subdomains: {ex.Message}");
        }
        finally
        {
            this.isLoading = false;
            this.StateHasChanged();
        }
    }

    private void NavigateToSubdomainDetails(Guid subdomainId)
    {
        if (this.DomainId.HasValue)
        {
            navigation.NavigateTo($"/admin/domains/{this.DomainId}/subdomains/{subdomainId}");
        }
        else
        {
            navigation.NavigateTo($"/admin/subdomains/{subdomainId}");
        }
    }

    private sealed class SubdomainSearchRequest
    {
        public string? SearchTerm { get; set; }

        public SubdomainStatus? Status { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string SortBy { get; set; } = "CreatedAt";

        public bool SortDescending { get; set; } = true;

        public Guid ParentDomainId { get; set; }
    }
}