using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace Spamma.App.Client.Pages.Admin;

public partial class SpamFeedbackSettings(HttpClient httpClient) : ComponentBase
{
    private bool loading = true;
    private bool saving;
    private bool saved;
    private bool canConfigure = true;
    private bool hasWebhookSecret;
    private string? arfRecipient;
    private string? webhookUrl;
    private string? newWebhookSecret;
    private string? error;

    [Parameter]
    public Guid SubdomainId { get; set; }

    private string Endpoint => $"api/email-inbox/subdomains/{this.SubdomainId}/spam-feedback";

    protected override async Task OnInitializedAsync()
    {
        using var response = await httpClient.GetAsync(this.Endpoint);
        if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
        {
            this.canConfigure = false;
            this.error = "You do not have permission to configure feedback for this subdomain.";
        }
        else if (response.IsSuccessStatusCode)
        {
            var settings = await response.Content.ReadFromJsonAsync<SettingsResponse>();
            this.arfRecipient = settings?.ArfRecipient;
            this.webhookUrl = settings?.WebhookUrl;
            this.hasWebhookSecret = settings?.HasWebhookSecret ?? false;
        }
        else
        {
            this.error = "Feedback settings could not be loaded.";
        }

        this.loading = false;
    }

    private async Task SaveAsync()
    {
        this.saving = true;
        this.saved = false;
        this.error = null;
        this.newWebhookSecret = null;
        using var response = await httpClient.PutAsJsonAsync(this.Endpoint, new SettingsRequest(this.arfRecipient, this.webhookUrl));
        if (response.IsSuccessStatusCode)
        {
            var settings = await response.Content.ReadFromJsonAsync<SettingsResponse>();
            this.hasWebhookSecret = settings?.HasWebhookSecret ?? false;
            this.newWebhookSecret = settings?.NewWebhookSecret;
            this.saved = true;
        }
        else
        {
            this.error = response.StatusCode == HttpStatusCode.BadRequest
                ? "Check that the destinations belong to the verified parent domain and use HTTPS for the webhook."
                : "Feedback settings could not be saved.";
        }

        this.saving = false;
    }

    private sealed record SettingsRequest(string? ArfRecipient, string? WebhookUrl);

    private sealed record SettingsResponse(string? ArfRecipient, string? WebhookUrl, bool HasWebhookSecret, string? NewWebhookSecret);
}
