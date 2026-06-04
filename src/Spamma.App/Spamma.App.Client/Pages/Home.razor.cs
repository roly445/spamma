using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.App.Client.Pages;

/// <summary>
/// Code-behind for the Home page.
/// </summary>
public partial class Home(
    IQueryRunner querier,
    ICommandRunner commander,
    ISignalRService signalRService,
    INotificationService notificationService,
    NavigationManager navigationManager) : IDisposable
{
    private const int DefaultPageSize = 25;
    private static readonly Guid CatchAllSubdomainId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private IReadOnlyList<SearchEmailsQueryResult.EmailSummary> emails = new List<SearchEmailsQueryResult.EmailSummary>();
    private SearchEmailsQueryResult.EmailSummary? selectedEmail;
    private string _searchText = string.Empty;
    private bool _isLoading = false;
    private bool _isSearching = false;
    private bool _showCampaignEmails = false;
    private System.Timers.Timer? _searchTimer;
    private SearchEmailsQueryResult? _searchResult;

    private GetCampaignsQueryResult? _campaigns;
    private List<SubdomainSummary>? _subdomains;
    private string _selectedSubdomainId = string.Empty;
    private string _campaignSortBy = "LastReceivedAt";
    private bool _campaignSortDescending = true;
    private int _campaignCurrentPage = 1;
    private int _campaignPageSize = 50;
    private bool _campaignsLoading;
    private bool _campaignDetailLoading;
    private string? _deletingCampaignValue;
    private GetCampaignsQueryResult.CampaignSummary? _selectedCampaign;
    private GetCampaignDetailQueryResult? _campaignDetail;
    private SearchEmailsQueryResult.EmailSummary? _campaignEmailSummary;
    private bool _campaignsInitialized;
    private bool _disposed;

    private enum InboxTab
    {
        Inbox,
        CatchAll,
        Campaigns,
    }

    [Parameter]
    public Guid CampaignId { get; set; }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed)
        {
            return;
        }

        if (disposing)
        {
            navigationManager.LocationChanged -= this.OnLocationChanged;
            signalRService.OnNewEmailReceived -= this.OnNewEmailReceived;
            signalRService.OnCatchAllEmailReceived -= this.OnCatchAllEmailReceived;
            signalRService.OnEmailDeleted -= this.OnEmailDeleted;
            signalRService.OnEmailUpdated -= this.OnEmailUpdated;
            this._searchTimer?.Dispose();
        }

        this._disposed = true;
    }

    protected override async Task OnInitializedAsync()
    {
        navigationManager.LocationChanged += this.OnLocationChanged;

        if (this.GetActiveTab() == InboxTab.Campaigns)
        {
            await this.EnsureCampaignsLoaded();
        }
        else if (this.GetActiveTab() == InboxTab.Inbox)
        {
            await this.LoadEmails();
        }

        signalRService.OnNewEmailReceived += this.OnNewEmailReceived;
        signalRService.OnCatchAllEmailReceived += this.OnCatchAllEmailReceived;
        signalRService.OnEmailDeleted += this.OnEmailDeleted;
        signalRService.OnEmailUpdated += this.OnEmailUpdated;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (this.GetActiveTab() == InboxTab.Campaigns)
        {
            await this.EnsureCampaignsLoaded();
            await this.ApplyCampaignRouteSelection();
        }
    }

    private static MarkupString HighlightSearchTerm(string text, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || string.IsNullOrWhiteSpace(text))
        {
            return new MarkupString(System.Web.HttpUtility.HtmlEncode(text));
        }

        var highlightedText = text.Replace(
            searchTerm,
            $"<mark class=\"bg-yellow-200 text-yellow-900\">{searchTerm}</mark>",
            StringComparison.OrdinalIgnoreCase);

        return new MarkupString(highlightedText);
    }

    private static IEnumerable<int> GetVisiblePageNumbers(int currentPage, int totalPages)
    {
        if (totalPages <= 7)
        {
            return Enumerable.Range(1, totalPages);
        }

        var visiblePages = new List<int> { 1 };

        if (currentPage <= 4)
        {
            visiblePages.AddRange(Enumerable.Range(2, 4));
            visiblePages.Add(totalPages);
        }
        else if (currentPage >= totalPages - 3)
        {
            visiblePages.AddRange(Enumerable.Range(totalPages - 4, 4));
            visiblePages.Add(totalPages);
        }
        else
        {
            visiblePages.AddRange(Enumerable.Range(currentPage - 1, 3));
            visiblePages.Add(totalPages);
        }

        return visiblePages.Distinct().OrderBy(p => p);
    }

    private string GetPageTitle() =>
        this.GetActiveTab() switch
        {
            InboxTab.CatchAll => "Catch-All Inbox - Spamma",
            InboxTab.Campaigns => "Email Campaigns - Spamma",
            _ => "Spamma - Email Client",
        };

    private string GetTabClasses(InboxTab tab)
    {
        const string baseClasses = "inline-flex h-10 w-10 items-center justify-center rounded-md border text-sm transition-colors duration-150";

        return this.GetActiveTab() == tab
            ? $"{baseClasses} border-blue-200 bg-blue-50 text-blue-700"
            : $"{baseClasses} border-transparent text-gray-500 hover:border-gray-200 hover:bg-gray-50 hover:text-gray-700";
    }

    private InboxTab GetActiveTab()
    {
        var path = navigationManager.ToBaseRelativePath(navigationManager.Uri).TrimEnd('/');

        if (path.StartsWith("m/campaigns", StringComparison.OrdinalIgnoreCase))
        {
            return InboxTab.Campaigns;
        }

        return path.Equals("m/catch-all", StringComparison.OrdinalIgnoreCase)
            ? InboxTab.CatchAll
            : InboxTab.Inbox;
    }

    private void NavigateToTab(InboxTab tab)
    {
        var target = tab switch
        {
            InboxTab.CatchAll => "/m/catch-all",
            InboxTab.Campaigns => "/m/campaigns",
            _ => "/m/inbox",
        };

        navigationManager.NavigateTo(target);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _ = this.InvokeAsync(async () =>
        {
            if (this.GetActiveTab() == InboxTab.Campaigns)
            {
                await this.EnsureCampaignsLoaded();
            }
            else if (this.GetActiveTab() == InboxTab.Inbox && this._searchResult == null)
            {
                await this.LoadEmails();
            }

            this.StateHasChanged();
        });
    }

    private async Task LoadEmails(string? searchText = null, int page = 1)
    {
        this._isLoading = true;
        this.StateHasChanged();

        try
        {
            var result = await querier.Send(new SearchEmailsQuery(searchText, page, DefaultPageSize, this._showCampaignEmails));
            if (result.Status == QueryResultStatus.Succeeded && result.Data != null)
            {
                this._searchResult = result.Data;
                this.emails = result.Data.Items;

                if (this.selectedEmail != null && this.emails.All(e => e.EmailId != this.selectedEmail.EmailId))
                {
                    this.selectedEmail = null;
                }
            }
        }
        finally
        {
            this._isLoading = false;
            this._isSearching = false;
            this.StateHasChanged();
        }
    }

    private async Task RefreshEmails()
    {
        var currentPage = this._searchResult?.Page ?? 1;
        await this.LoadEmails(this._searchText, currentPage);
    }

    private async Task GoToEmailPage(int page)
    {
        if (page < 1 || (this._searchResult != null && page > this._searchResult.TotalPages) || this._isLoading)
        {
            return;
        }

        await this.LoadEmails(this._searchText, page);
    }

    private async Task ToggleCampaignFilter(ChangeEventArgs e)
    {
        this._showCampaignEmails = (bool)(e.Value ?? false);
        await this.LoadEmails(this._searchText, 1);
    }

    private IEnumerable<int> GetVisiblePageNumbers()
    {
        if (this._searchResult == null)
        {
            return [];
        }

        return GetVisiblePageNumbers(this._searchResult.Page, this._searchResult.TotalPages);
    }

    private IEnumerable<int> GetVisibleCampaignPageNumbers()
    {
        if (this._campaigns == null)
        {
            return [];
        }

        return GetVisiblePageNumbers(this._campaigns.Page, this._campaigns.TotalPages);
    }

    private void HandleEmailSelected(SearchEmailsQueryResult.EmailSummary email)
    {
        this.selectedEmail = email;
        this.StateHasChanged();
    }

    private async Task HandleEmailDeleted(SearchEmailsQueryResult.EmailSummary deletedEmail)
    {
        this.emails = this.emails.Where(e => e.EmailId != deletedEmail.EmailId).ToList();

        if (this._searchResult != null)
        {
            this._searchResult = this._searchResult with
            {
                Items = this.emails,
                TotalCount = this._searchResult.TotalCount - 1,
            };
        }

        if (this.selectedEmail?.EmailId == deletedEmail.EmailId)
        {
            this.selectedEmail = null;
        }

        if (this.emails.Count == 0 && this._searchResult != null && this._searchResult.Page > 1)
        {
            await this.GoToEmailPage(this._searchResult.Page - 1);
        }

        this.StateHasChanged();
    }

    private void HandleEmailUpdated(SearchEmailsQueryResult.EmailSummary updatedEmail)
    {
        this.emails = this.emails.Select(email =>
            email.EmailId == updatedEmail.EmailId ? updatedEmail : email).ToList();

        if (this.selectedEmail?.EmailId == updatedEmail.EmailId)
        {
            this.selectedEmail = updatedEmail;
        }

        if (this._searchResult != null)
        {
            var updatedItems = this._searchResult.Items.Select(email =>
                email.EmailId == updatedEmail.EmailId ? updatedEmail : email).ToList();

            this._searchResult = this._searchResult with
            {
                Items = updatedItems,
            };
        }

        this.StateHasChanged();
    }

    private async Task HandleSearchKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await this.PerformSearch();
        }
        else
        {
            this._searchTimer?.Stop();
            this._searchTimer = new System.Timers.Timer(300);
            this._searchTimer.Elapsed += async (sender, args) =>
            {
                this._searchTimer.Stop();
                await this.InvokeAsync(async () => await this.PerformSearch());
            };
            this._searchTimer.Start();
        }
    }

    private async Task PerformSearch()
    {
        this._isSearching = true;
        this.StateHasChanged();

        await this.LoadEmails(string.IsNullOrWhiteSpace(this._searchText) ? null : this._searchText, 1);
    }

    private async Task ClearSearch()
    {
        this._searchText = string.Empty;
        await this.PerformSearch();
    }

    private string GetEmailItemClasses(SearchEmailsQueryResult.EmailSummary email)
    {
        var baseClasses = "relative border-b border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors duration-150";

        if (email == this.selectedEmail)
        {
            baseClasses += " bg-blue-50 border-blue-200";
        }

        return baseClasses;
    }

    private async Task EnsureCampaignsLoaded()
    {
        if (this._campaignsInitialized)
        {
            return;
        }

        this._campaignsInitialized = true;
        await this.LoadSubdomains();
    }

    private async Task LoadSubdomains()
    {
        this._campaignsLoading = true;
        this.StateHasChanged();

        try
        {
            var query = new SearchSubdomainsQuery(null, null, null, 1, 1000, "SubdomainName", false);
            var result = await querier.Send(query, CancellationToken.None);

            if (result.Status == QueryResultStatus.Succeeded && result.Data != null)
            {
                this._subdomains =
                [
                    new SubdomainSummary
                    {
                        Id = CatchAllSubdomainId,
                        SubdomainName = "Catch-All",
                    },
                    .. result.Data.Items.Select(s => new SubdomainSummary
                    {
                        Id = s.SubdomainId,
                        SubdomainName = s.SubdomainName,
                    }),
                ];

                if (this._subdomains.Count > 0)
                {
                    this._selectedSubdomainId = this._subdomains[0].Id.ToString();
                    await this.RefreshCampaigns();
                }
            }
        }
        finally
        {
            this._campaignsLoading = false;
            this.StateHasChanged();
        }
    }

    private async Task OnCampaignSubdomainChanged(ChangeEventArgs e)
    {
        if (e.Value is not string subdomainId || string.IsNullOrEmpty(subdomainId))
        {
            return;
        }

        this._selectedSubdomainId = subdomainId;
        this._campaignCurrentPage = 1;
        this.ClearSelectedCampaign();
        navigationManager.NavigateTo("/m/campaigns");
        await this.RefreshCampaigns();
    }

    private async Task OnCampaignSortChanged(ChangeEventArgs e)
    {
        if (e.Value is string sortBy)
        {
            this._campaignSortBy = sortBy;
            this._campaignCurrentPage = 1;
            await this.RefreshCampaigns();
        }
    }

    private async Task RefreshCampaigns()
    {
        if (string.IsNullOrEmpty(this._selectedSubdomainId) || !Guid.TryParse(this._selectedSubdomainId, out var subdomainId))
        {
            return;
        }

        this._campaignsLoading = true;
        this.StateHasChanged();

        try
        {
            var query = new GetCampaignsQuery(
                subdomainId,
                this._campaignCurrentPage,
                this._campaignPageSize,
                this._campaignSortBy,
                this._campaignSortDescending);

            var result = await querier.Send(query, CancellationToken.None);
            if (result.Status == QueryResultStatus.Succeeded && result.Data != null)
            {
                this._campaigns = result.Data;

                if (this._selectedCampaign != null && this._campaigns.Items.All(c => c.CampaignId != this._selectedCampaign.CampaignId))
                {
                    this.ClearSelectedCampaign();
                }
            }
        }
        finally
        {
            this._campaignsLoading = false;
            this.StateHasChanged();
        }
    }

    private async Task PreviousCampaignPage()
    {
        if (this._campaigns?.HasPreviousPage == true)
        {
            this._campaignCurrentPage--;
            await this.RefreshCampaigns();
        }
    }

    private async Task NextCampaignPage()
    {
        if (this._campaigns?.HasNextPage == true)
        {
            this._campaignCurrentPage++;
            await this.RefreshCampaigns();
        }
    }

    private async Task GoToCampaignPage(int pageNumber)
    {
        if (pageNumber < 1 || (this._campaigns != null && pageNumber > this._campaigns.TotalPages) || this._campaignsLoading)
        {
            return;
        }

        this._campaignCurrentPage = pageNumber;
        await this.RefreshCampaigns();
    }

    private async Task HandleCampaignSelected(GetCampaignsQueryResult.CampaignSummary campaign)
    {
        this._selectedCampaign = campaign;
        navigationManager.NavigateTo($"/m/campaigns/{campaign.CampaignId}");
        await this.LoadCampaignDetail(campaign.CampaignId);
    }

    private async Task ApplyCampaignRouteSelection()
    {
        if (this.CampaignId == Guid.Empty)
        {
            this.ClearSelectedCampaign();
            return;
        }

        this._selectedCampaign = this._campaigns?.Items.FirstOrDefault(c => c.CampaignId == this.CampaignId);

        if (this._campaignDetail?.CampaignId != this.CampaignId)
        {
            await this.LoadCampaignDetail(this.CampaignId);
        }
    }

    private async Task LoadCampaignDetail(Guid campaignId)
    {
        this._campaignDetailLoading = true;
        this._campaignEmailSummary = null;
        this.StateHasChanged();

        try
        {
            var result = await querier.Send(new GetCampaignDetailQuery(campaignId), CancellationToken.None);

            if (result.Status == QueryResultStatus.Succeeded && result.Data != null)
            {
                this._campaignDetail = result.Data;

                if (result.Data.Sample != null)
                {
                    var emailResult = await querier.Send(new GetEmailByIdQuery(result.Data.Sample.MessageId), CancellationToken.None);
                    if (emailResult.Status == QueryResultStatus.Succeeded)
                    {
                        this._campaignEmailSummary = new SearchEmailsQueryResult.EmailSummary(
                            emailResult.Data.Id,
                            emailResult.Data.Subject,
                            "Email",
                            emailResult.Data.WhenSent,
                            emailResult.Data.IsFavorite,
                            emailResult.Data.CampaignId,
                            result.Data.CampaignValue);
                    }
                }
            }
            else
            {
                this.ClearSelectedCampaign();
            }
        }
        finally
        {
            this._campaignDetailLoading = false;
            this.StateHasChanged();
        }
    }

    private async Task DeleteCampaign(Guid campaignId, string campaignValue)
    {
        if (this._deletingCampaignValue != null)
        {
            return;
        }

        this._deletingCampaignValue = campaignValue;
        this.StateHasChanged();

        try
        {
            var result = await commander.Send(new DeleteCampaignCommand(campaignId));

            if (result.Status == CommandResultStatus.Succeeded)
            {
                if (this._campaigns != null && this._campaignCurrentPage > 1 && this._campaigns.Items.Count <= 1)
                {
                    this._campaignCurrentPage--;
                }

                if (this._selectedCampaign?.CampaignId == campaignId || this._campaignDetail?.CampaignId == campaignId)
                {
                    this.ClearSelectedCampaign();
                    navigationManager.NavigateTo("/m/campaigns");
                }

                await this.RefreshCampaigns();
                notificationService.ShowSuccess($"Campaign '{campaignValue}' deleted successfully");
            }
            else
            {
                notificationService.ShowError($"Failed to delete campaign '{campaignValue}'. Please try again.");
            }
        }
        catch (Exception ex)
        {
            notificationService.ShowError($"An error occurred while deleting the campaign: {ex.Message}");
        }
        finally
        {
            this._deletingCampaignValue = null;
            this.StateHasChanged();
        }
    }

    private void ClearSelectedCampaign()
    {
        this._selectedCampaign = null;
        this._campaignDetail = null;
        this._campaignEmailSummary = null;
    }

    private string GetCampaignItemClasses(GetCampaignsQueryResult.CampaignSummary campaign)
    {
        var baseClasses = "block w-full border-b border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors duration-150";

        if (this._selectedCampaign?.CampaignId == campaign.CampaignId || this._campaignDetail?.CampaignId == campaign.CampaignId)
        {
            baseClasses += " bg-blue-50 border-blue-200";
        }

        if (this._deletingCampaignValue == campaign.CampaignValue)
        {
            baseClasses += " opacity-50";
        }

        return baseClasses;
    }

    private string GetDeleteCampaignClasses(string campaignValue)
    {
        var baseClasses = "inline-flex text-sm font-medium text-red-600 hover:text-red-900";

        return this._deletingCampaignValue == null || this._deletingCampaignValue == campaignValue
            ? baseClasses
            : $"{baseClasses} opacity-50 pointer-events-none";
    }

    private async Task OnNewEmailReceived()
    {
        await this.InvokeAsync(async () =>
        {
            if (this.GetActiveTab() == InboxTab.Campaigns)
            {
                await this.RefreshCampaignsFromRealtime();
            }
            else
            {
                await this.LoadEmails();
            }

            this.StateHasChanged();
        });
    }

    private async Task OnCatchAllEmailReceived()
    {
        await this.InvokeAsync(async () =>
        {
            if (this.GetActiveTab() == InboxTab.Campaigns)
            {
                await this.RefreshCampaignsFromRealtime();
            }

            this.StateHasChanged();
        });
    }

    private async Task RefreshCampaignsFromRealtime()
    {
        await this.RefreshCampaigns();

        var selectedCampaignId = this._campaignDetail?.CampaignId ?? this._selectedCampaign?.CampaignId;
        if (selectedCampaignId.HasValue)
        {
            await this.LoadCampaignDetail(selectedCampaignId.Value);
        }
    }

    private async Task OnEmailDeleted()
    {
        await this.InvokeAsync(async () =>
        {
            if (this.GetActiveTab() == InboxTab.Inbox)
            {
                await this.LoadEmails();
            }

            this.StateHasChanged();
        });
    }

    private async Task OnEmailUpdated()
    {
        await this.InvokeAsync(async () =>
        {
            if (this.GetActiveTab() == InboxTab.Inbox)
            {
                await this.LoadEmails();
            }

            this.StateHasChanged();
        });
    }

    private sealed class SubdomainSummary
    {
        public Guid Id { get; set; }

        public string SubdomainName { get; set; } = string.Empty;
    }
}
