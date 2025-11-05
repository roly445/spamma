using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.App.Client.Pages;

/// <summary>
/// Code-behind for the Campaigns razor component.
/// </summary>
[Authorize]
public partial class Campaigns
{
    private GetCampaignsQueryResult? _campaigns;
    private List<SubdomainSummary>? _subdomains;
    private string _selectedSubdomainId = string.Empty;
    private string _sortBy = "LastReceivedAt";
    private bool _isSortDescending = true;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private bool _isLoading;

    [Inject]
    public IQuerier Querier { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await this.LoadSubdomains();
    }

    private async Task LoadSubdomains()
    {
        try
        {
            var query = new SearchSubdomainsQuery(null, null, null, 1, 1000, "SubdomainName", false);
            var result = await this.Querier.Send(query, CancellationToken.None);

            if (result.Status == QueryResultStatus.Succeeded)
            {
                this._subdomains = result.Data.Items
                    .Select(s => new SubdomainSummary
                    {
                        Id = s.Id,
                        SubdomainName = s.SubdomainName,
                    })
                    .ToList();

                if (this._subdomains.Any())
                {
                    this._selectedSubdomainId = this._subdomains[0].Id.ToString();
                    await this.RefreshCampaigns();
                }
            }
        }
        catch (Exception)
        {
            // Handle error silently or show notification
        }
    }

    private async Task OnSubdomainChanged(ChangeEventArgs e)
    {
        if (e.Value is string subdomainId && !string.IsNullOrEmpty(subdomainId))
        {
            this._selectedSubdomainId = subdomainId;
            this._currentPage = 1;
            await this.RefreshCampaigns();
        }
    }

    private async Task OnSortChanged(ChangeEventArgs e)
    {
        if (e.Value is string sortBy)
        {
            this._sortBy = sortBy;
            this._currentPage = 1;
            await this.RefreshCampaigns();
        }
    }

    private async Task RefreshCampaigns()
    {
        if (string.IsNullOrEmpty(this._selectedSubdomainId) || !Guid.TryParse(this._selectedSubdomainId, out var subdomainId))
        {
            return;
        }

        this._isLoading = true;
        try
        {
            var query = new GetCampaignsQuery(
                subdomainId,
                this._currentPage,
                this._pageSize,
                this._sortBy,
                this._isSortDescending);

            var result = await this.Querier.Send(query, CancellationToken.None);
            if (result.Status == QueryResultStatus.Succeeded)
            {
                this._campaigns = result.Data;
            }
        }
        catch (Exception)
        {
            // Handle error silently or show notification
        }
        finally
        {
            this._isLoading = false;
        }
    }

    private async Task PreviousPage()
    {
        if (this._currentPage > 1)
        {
            this._currentPage--;
            await this.RefreshCampaigns();
        }
    }

    private async Task NextPage()
    {
        if (this._campaigns != null && this._currentPage < this._campaigns.TotalPages)
        {
            this._currentPage++;
            await this.RefreshCampaigns();
        }
    }

    private async Task GoToPage(int pageNumber)
    {
        this._currentPage = pageNumber;
        await this.RefreshCampaigns();
    }

    private sealed class SubdomainSummary
    {
        public Guid Id { get; set; }

        public string SubdomainName { get; set; } = string.Empty;
    }
}
