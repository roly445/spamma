using BluQube.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages;

[Authorize]
public partial class CampaignDetail
{
    private GetCampaignDetailQueryResult? _campaignDetail;
    private string _campaignValue = string.Empty;
    private bool _isLoading;

    [Parameter]
    public Guid CampaignId { get; set; }

    [SupplyParameterFromQuery]
    public Guid? SubdomainId { get; set; }

    [Inject]
    public IQuerier Querier { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!SubdomainId.HasValue || SubdomainId.Value == Guid.Empty)
        {
            NavigationManager.NavigateTo("/campaigns");
            return;
        }

        await LoadCampaignDetail();
    }

    private async Task LoadCampaignDetail()
    {
        _isLoading = true;
        try
        {
            var query = new GetCampaignDetailQuery(SubdomainId!.Value, CampaignId);
            var result = await Querier.Send(query, CancellationToken.None);

            if (result?.Data != null)
            {
                _campaignDetail = result.Data;
                _campaignValue = result.Data.CampaignValue;
            }
            else
            {
                NavigationManager.NavigateTo("/campaigns");
            }
        }
        catch (Exception)
        {
            NavigationManager.NavigateTo("/campaigns");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private string GetDurationText()
    {
        if (_campaignDetail == null)
        {
            return "N/A";
        }

        var duration = _campaignDetail.LastReceivedAt - _campaignDetail.FirstReceivedAt;
        if (duration.TotalDays > 1)
        {
            return $"{(int)duration.TotalDays} days";
        }

        if (duration.TotalHours > 1)
        {
            return $"{(int)duration.TotalHours}h";
        }

        return $"{(int)duration.TotalMinutes}m";
    }
}
