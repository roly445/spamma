using BluQube.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages;

[Authorize]
public partial class Campaigns
{
    [Inject]
    public IQuerier Querier { get; set; } = null!;

    private GetCampaignsQueryResult? _campaigns;
    private List<SubdomainSummary>? _subdomains;
    private string _selectedSubdomainId = string.Empty;
    private string _sortBy = "LastReceivedAt";
    private bool _isSortDescending = true;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private bool _isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadSubdomains();
    }

    private async Task LoadSubdomains()
    {
        try
        {
            var query = new SearchSubdomainsQuery(null, null, null, 1, 1000, "SubdomainName", false);
            var result = await this.Querier.Send(query, CancellationToken.None);

            if (result?.Data != null)
            {
                _subdomains = result.Data.Items
                    .Cast<SearchSubdomainsQueryResult.SubdomainSummary>()
                    .Select(s => new SubdomainSummary
                    {
                        Id = s.Id,
                        SubdomainName = s.SubdomainName,
                    })
                    .ToList();

                if (_subdomains.Any())
                {
                    _selectedSubdomainId = _subdomains.First().Id.ToString();
                    await RefreshCampaigns();
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
            _selectedSubdomainId = subdomainId;
            _currentPage = 1;
            await RefreshCampaigns();
        }
    }

    private async Task OnSortChanged(ChangeEventArgs e)
    {
        if (e.Value is string sortBy)
        {
            _sortBy = sortBy;
            _currentPage = 1;
            await RefreshCampaigns();
        }
    }

    private async Task RefreshCampaigns()
    {
        if (string.IsNullOrEmpty(_selectedSubdomainId) || !Guid.TryParse(_selectedSubdomainId, out var subdomainId))
        {
            return;
        }

        _isLoading = true;
        try
        {
            var query = new GetCampaignsQuery(
                subdomainId,
                _currentPage,
                _pageSize,
                _sortBy,
                _isSortDescending);

            var result = await Querier.Send(query, CancellationToken.None);
            if (result?.Data != null)
            {
                _campaigns = result.Data;
            }
        }
        catch (Exception)
        {
            // Handle error silently or show notification
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task PreviousPage()
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            await RefreshCampaigns();
        }
    }

    private async Task NextPage()
    {
        if (_campaigns != null && _currentPage < _campaigns.TotalPages)
        {
            _currentPage++;
            await RefreshCampaigns();
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
