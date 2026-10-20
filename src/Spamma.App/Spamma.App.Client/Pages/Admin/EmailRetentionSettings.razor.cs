using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.App.Client.Pages.Admin;

public partial class EmailRetentionSettings(HttpClient httpClient) : ComponentBase
{
    private bool loading = true;
    private bool saving;
    private bool saved;
    private bool canConfigure = true;
    private bool ageEnabled;
    private bool countEnabled;
    private int? maxAgeHours;
    private int? maxMessages;
    private string? error;
    private EmailRetentionMode selectedMode;
    private EmailRetentionPolicyView? policy;

    [Parameter]
    public Guid DomainId { get; set; }

    [Parameter]
    public Guid SubdomainId { get; set; }

    private bool IsSubdomain => this.SubdomainId != Guid.Empty;

    private bool IsOverride => !this.IsSubdomain || this.selectedMode == EmailRetentionMode.Override;

    private int? SelectedMaxAgeHours => this.ageEnabled ? this.maxAgeHours : null;

    private int? SelectedMaxMessages => this.countEnabled ? this.maxMessages : null;

    private bool HasPendingChange => this.policy is not null &&
        (this.selectedMode != this.policy.Mode ||
         (this.IsOverride &&
          (this.SelectedMaxAgeHours != this.policy.MaxAgeHours || this.SelectedMaxMessages != this.policy.MaxMessages)));

    private string Endpoint => this.IsSubdomain
        ? $"api/email-inbox/subdomains/{this.SubdomainId}/retention"
        : $"api/email-inbox/domains/{this.DomainId}/retention";

    private string BackUrl => this.IsSubdomain
        ? $"/admin/subdomains/{this.SubdomainId}"
        : $"/admin/domains/{this.DomainId}";

    protected override async Task OnParametersSetAsync()
    {
        this.loading = true;
        this.canConfigure = true;
        this.error = null;
        this.saved = false;
        using var response = await httpClient.GetAsync(this.Endpoint);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            this.canConfigure = false;
            this.error = "You do not have permission to manage retention for this inbox.";
        }
        else if (response.IsSuccessStatusCode)
        {
            this.policy = await response.Content.ReadFromJsonAsync<EmailRetentionPolicyView>();
            this.SetInputs();
        }
        else
        {
            this.error = "Retention settings could not be loaded.";
        }

        this.loading = false;
    }

    private void SetInputs()
    {
        if (this.policy is null)
        {
            return;
        }

        this.selectedMode = this.policy.Mode;
        var initialAge = this.policy.Mode == EmailRetentionMode.Override
            ? this.policy.MaxAgeHours : this.policy.EffectiveMaxAgeHours;
        var initialCount = this.policy.Mode == EmailRetentionMode.Override
            ? this.policy.MaxMessages : this.policy.EffectiveMaxMessages;
        this.ageEnabled = initialAge.HasValue;
        this.countEnabled = initialCount.HasValue;
        this.maxAgeHours = initialAge ?? 24;
        this.maxMessages = initialCount ?? 1000;
    }

    private async Task SaveAsync()
    {
        if (!this.HasPendingChange || this.saving)
        {
            return;
        }

        this.saving = true;
        this.saved = false;
        this.error = null;
        var request = new EmailRetentionPolicyRequest(
            this.selectedMode,
            this.IsOverride ? this.SelectedMaxAgeHours : null,
            this.IsOverride ? this.SelectedMaxMessages : null);
        using var response = await httpClient.PutAsJsonAsync(this.Endpoint, request);
        if (response.IsSuccessStatusCode)
        {
            this.policy = await response.Content.ReadFromJsonAsync<EmailRetentionPolicyView>();
            this.SetInputs();
            this.saved = true;
        }
        else
        {
            this.error = response.StatusCode == HttpStatusCode.BadRequest
                ? "Age must be 1–87,600 hours and count must be 1–1,000,000 when enabled."
                : "Retention settings could not be saved.";
        }

        this.saving = false;
    }
}
