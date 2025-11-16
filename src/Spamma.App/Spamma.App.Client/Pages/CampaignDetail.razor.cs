using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.App.Client.Pages;

[Authorize]
public partial class CampaignDetail(IQuerier querier, NavigationManager navigationManager)
{
    private GetCampaignDetailQueryResult? _campaignDetail;
    private SearchEmailsQueryResult.EmailSummary? _emailSummary;
    private string _campaignValue = string.Empty;
    private bool _isLoading;

    [Parameter]
    public Guid CampaignId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (this.CampaignId == Guid.Empty)
        {
            navigationManager.NavigateTo("/campaigns");
            return;
        }

        await this.LoadCampaignDetail();
    }

    private async Task LoadCampaignDetail()
    {
        this._isLoading = true;
        try
        {
            // We don't know the SubdomainId yet, so we'll use Guid.Empty for now
            // The query processor should handle this by looking up the campaign first
            var query = new GetCampaignDetailQuery(Guid.Empty, this.CampaignId);
            var result = await querier.Send(query, CancellationToken.None);

            if (result.Status == QueryResultStatus.Succeeded)
            {
                this._campaignDetail = result.Data;
                this._campaignValue = result.Data.CampaignValue;

                // If there's a sample message, fetch the full email summary
                if (result.Data.Sample != null)
                {
                    var emailQuery = new GetEmailByIdQuery(result.Data.Sample.MessageId);
                    var emailResult = await querier.Send(emailQuery, CancellationToken.None);

                    if (emailResult.Status == QueryResultStatus.Succeeded)
                    {
                        this._emailSummary = new SearchEmailsQueryResult.EmailSummary(
                            emailResult.Data.Id,
                            emailResult.Data.Subject,
                            "Email",
                            emailResult.Data.WhenSent,
                            emailResult.Data.IsFavorite,
                            emailResult.Data.CampaignId,
                            this._campaignDetail?.CampaignValue);
                    }
                }
            }
            else
            {
                navigationManager.NavigateTo("/campaigns");
            }
        }
        catch (Exception)
        {
            navigationManager.NavigateTo("/campaigns");
        }
        finally
        {
            this._isLoading = false;
        }
    }
}