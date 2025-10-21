﻿using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.App.Client.Pages;

/// <summary>
/// Backing code for the home page.
/// </summary>
public partial class Home(IQuerier querier)
{
    private const int DefaultPageSize = 25;
    private IReadOnlyList<SearchEmailsQueryResult.EmailSummary> emails = new List<SearchEmailsQueryResult.EmailSummary>();
    private SearchEmailsQueryResult.EmailSummary? selectedEmail;
    private string _searchText = string.Empty;
    private bool _isLoading = false;
    private bool _isSearching = false;
    private System.Timers.Timer? _searchTimer;
    private SearchEmailsQueryResult? _searchResult;

    public void Dispose()
    {
        this._searchTimer?.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        await this.LoadEmails();
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

    private async Task LoadEmails(string? searchText = null, int page = 1)
    {
        this._isLoading = true;
        this.StateHasChanged();

        try
        {
            var result = await querier.Send(new SearchEmailsQuery(searchText, page, DefaultPageSize));
            if (result.Status == QueryResultStatus.Succeeded)
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

    private async Task GoToPage(int page)
    {
        if (page < 1 || (this._searchResult != null && page > this._searchResult.TotalPages) || this._isLoading)
        {
            return;
        }

        await this.LoadEmails(this._searchText, page);
    }

    private IEnumerable<int> GetVisiblePageNumbers()
    {
        if (this._searchResult == null)
        {
            return [];
        }

        var currentPage = this._searchResult.Page;
        var totalPages = this._searchResult.TotalPages;
        var visiblePages = new List<int>();

        if (totalPages <= 7)
        {
            // Show all pages if there are 7 or fewer
            return Enumerable.Range(1, totalPages);
        }

        // Always show first page
        visiblePages.Add(1);

        if (currentPage <= 4)
        {
            // Show pages 1-5 and last page
            visiblePages.AddRange(Enumerable.Range(2, 4));
            if (totalPages > 6) visiblePages.Add(-1); // -1 represents ellipsis
            visiblePages.Add(totalPages);
        }
        else if (currentPage >= totalPages - 3)
        {
            // Show first page, ellipsis, and last 5 pages
            if (totalPages > 6) visiblePages.Add(-1);
            visiblePages.AddRange(Enumerable.Range(totalPages - 4, 4));
            visiblePages.Add(totalPages);
        }
        else
        {
            // Show first page, ellipsis, current page ± 1, ellipsis, last page
            if (currentPage > 3)
            {
                visiblePages.Add(-1);
            }

            visiblePages.AddRange(Enumerable.Range(currentPage - 1, 3));
            if (currentPage < totalPages - 2)
            {
                visiblePages.Add(-1);
            }

            visiblePages.Add(totalPages);
        }

        return visiblePages.Where(p => p > 0); // Filter out ellipsis for now
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
            await this.GoToPage(this._searchResult.Page - 1);
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
            // Debounce search - wait 300ms after user stops typing
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
}