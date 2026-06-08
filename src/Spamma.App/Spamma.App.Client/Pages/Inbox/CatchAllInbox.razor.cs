using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.Common.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Inbox;

public partial class CatchAllInbox(
    IQueryRunner querier,
    ISystemSettingsCache systemSettingsCache,
    IClientSessionContext clientSessionContext,
    ISignalRService signalRService) : ComponentBase, IDisposable
{
    private IReadOnlyList<GetCatchAllEmailsQueryResult.SenderGroup> _groups = [];
    private SearchEmailsQueryResult.EmailSummary? _selectedEmail;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private bool _isSearching;
    private bool _catchAllEnabled;
    private int _totalCount;
    private System.Timers.Timer? _searchTimer;
    private bool _disposed;

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
            systemSettingsCache.SettingsChanged -= this.OnSystemSettingsChanged;
            signalRService.OnCatchAllEmailReceived -= this.OnCatchAllEmailReceived;
            this._searchTimer?.Dispose();
        }

        this._disposed = true;
    }

    protected override async Task OnInitializedAsync()
    {
        this._catchAllEnabled = systemSettingsCache.Current.CatchAllModeEnabled;
        systemSettingsCache.SettingsChanged += this.OnSystemSettingsChanged;
        signalRService.OnCatchAllEmailReceived += this.OnCatchAllEmailReceived;

        await clientSessionContext.TrackBreadcrumbAsync(
            "catch-all.inbox.opened",
            new Dictionary<string, string?>
            {
                ["enabled"] = this._catchAllEnabled.ToString(),
            });

        if (this._catchAllEnabled)
        {
            await this.LoadEmails();
        }
    }

    private static SearchEmailsQueryResult.EmailSummary ToSearchEmailSummary(GetCatchAllEmailsQueryResult.EmailSummary email)
        => new(
            email.EmailId,
            email.Subject,
            email.PrimaryToAddress,
            email.ReceivedAt,
            email.IsFavorite,
            email.CampaignId,
            email.CampaignValue);

    private async Task LoadEmails()
    {
        this._isLoading = true;
        this.StateHasChanged();

        try
        {
            var searchText = string.IsNullOrWhiteSpace(this._searchText) ? null : this._searchText;
            var result = await querier.Send(new GetCatchAllEmailsQuery(SearchText: searchText));
            if (result.Status == QueryResultStatus.Succeeded)
            {
                this._groups = result.Data.Groups;
                this._totalCount = result.Data.TotalCount;

                await clientSessionContext.TrackBreadcrumbAsync(
                    "catch-all.load.succeeded",
                    new Dictionary<string, string?>
                    {
                        ["search_active"] = string.IsNullOrWhiteSpace(searchText) ? bool.FalseString : bool.TrueString,
                        ["group_count"] = this._groups.Count.ToString(),
                        ["total_count"] = this._totalCount.ToString(),
                    });

                if (this._selectedEmail != null &&
                    this._groups.SelectMany(group => group.Emails).All(email => email.EmailId != this._selectedEmail.EmailId))
                {
                    this._selectedEmail = null;
                }
            }
            else
            {
                await clientSessionContext.TrackBreadcrumbAsync(
                    "catch-all.load.failed",
                    new Dictionary<string, string?>
                    {
                        ["status"] = result.Status.ToString(),
                        ["search_active"] = string.IsNullOrWhiteSpace(searchText) ? bool.FalseString : bool.TrueString,
                    });
            }
        }
        finally
        {
            this._isLoading = false;
            this._isSearching = false;
            this.StateHasChanged();
        }
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

        await clientSessionContext.TrackBreadcrumbAsync(
            "catch-all.search.performed",
            new Dictionary<string, string?>
            {
                ["search_length"] = this._searchText.Length.ToString(),
                ["is_empty"] = string.IsNullOrWhiteSpace(this._searchText).ToString(),
            });

        await this.LoadEmails();
    }

    private async Task ClearSearch()
    {
        this._searchText = string.Empty;
        await this.PerformSearch();
    }

    private async Task HandleEmailSelected(GetCatchAllEmailsQueryResult.EmailSummary email)
    {
        this._selectedEmail = ToSearchEmailSummary(email);
        await clientSessionContext.TrackBreadcrumbAsync(
            "catch-all.email.selected",
            new Dictionary<string, string?>
            {
                ["email_id"] = email.EmailId.ToString(),
                ["campaign_email"] = email.CampaignId.HasValue.ToString(),
            });
        this.StateHasChanged();
    }

    private string GetEmailItemClasses(GetCatchAllEmailsQueryResult.EmailSummary email)
    {
        var baseClasses = "relative border-b border-gray-100 hover:bg-blue-50 cursor-pointer transition-colors duration-150";

        if (email.EmailId == this._selectedEmail?.EmailId)
        {
            baseClasses += " bg-blue-50 border-blue-200";
        }

        return baseClasses;
    }

    private Task HandleEmailDeleted(SearchEmailsQueryResult.EmailSummary deletedEmail)
    {
        this._groups = this._groups
            .Select(group => group with
            {
                Emails = group.Emails
                    .Where(email => email.EmailId != deletedEmail.EmailId)
                    .ToList(),
            })
            .Where(group => group.Emails.Count > 0)
            .ToList();

        if (this._selectedEmail?.EmailId == deletedEmail.EmailId)
        {
            this._selectedEmail = null;
        }

        this.StateHasChanged();
        return Task.CompletedTask;
    }

    private Task HandleEmailUpdated(SearchEmailsQueryResult.EmailSummary updatedEmail)
    {
        this._groups = this._groups
            .Select(group => group with
            {
                Emails = group.Emails
                    .Select(email => email.EmailId == updatedEmail.EmailId
                        ? email with { IsFavorite = updatedEmail.IsFavorite }
                        : email)
                    .ToList(),
            })
            .ToList();

        if (this._selectedEmail?.EmailId == updatedEmail.EmailId)
        {
            this._selectedEmail = updatedEmail;
        }

        this.StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task OnSystemSettingsChanged(GetSystemSettingsQueryResult settings)
    {
        await this.InvokeAsync(async () =>
        {
            var wasEnabled = this._catchAllEnabled;
            this._catchAllEnabled = settings.CatchAllModeEnabled;

            if (this._catchAllEnabled && !wasEnabled)
            {
                await this.LoadEmails();
            }
            else if (!this._catchAllEnabled)
            {
                this._groups = [];
                this._selectedEmail = null;
                this._totalCount = 0;
                this.StateHasChanged();
            }
        });
    }

    private async Task OnCatchAllEmailReceived()
    {
        await this.InvokeAsync(async () =>
        {
            if (this._catchAllEnabled)
            {
                await this.LoadEmails();
            }
        });
    }
}
