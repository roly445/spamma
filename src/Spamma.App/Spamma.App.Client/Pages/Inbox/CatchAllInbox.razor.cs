using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Spamma.App.Client.Infrastructure.Contracts;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Inbox;

public partial class CatchAllInbox(IQueryRunner querier, IOptions<Settings> settings) : ComponentBase
{
    private IReadOnlyList<GetCatchAllEmailsQueryResult.SenderGroup> _groups = [];
    private GetCatchAllEmailsQueryResult.EmailSummary? _selectedEmail;
    private bool _isLoading;
    private bool _catchAllEnabled;

    protected override async Task OnInitializedAsync()
    {
        this._catchAllEnabled = settings.Value.CatchAllModeEnabled;

        if (this._catchAllEnabled)
        {
            await this.LoadEmails();
        }
    }

    private async Task LoadEmails()
    {
        this._isLoading = true;
        this.StateHasChanged();

        try
        {
            var result = await querier.Send(new GetCatchAllEmailsQuery());
            if (result.Status == QueryResultStatus.Succeeded)
            {
                this._groups = result.Data.Groups;
            }
        }
        finally
        {
            this._isLoading = false;
            this.StateHasChanged();
        }
    }

    private void HandleEmailSelected(GetCatchAllEmailsQueryResult.EmailSummary email)
    {
        this._selectedEmail = email;
        this.StateHasChanged();
    }

    private string GetEmailItemClasses(GetCatchAllEmailsQueryResult.EmailSummary email)
    {
        var baseClasses = "relative border-b border-gray-100 hover:bg-blue-50 cursor-pointer transition-colors duration-150";

        if (email == this._selectedEmail)
        {
            baseClasses += " bg-blue-50 border-blue-200";
        }

        return baseClasses;
    }
}
