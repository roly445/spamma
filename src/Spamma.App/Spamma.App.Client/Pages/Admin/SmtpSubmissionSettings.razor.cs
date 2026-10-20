using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.App.Client.Pages.Admin;

public partial class SmtpSubmissionSettings(HttpClient httpClient, IJSRuntime jsRuntime) : ComponentBase
{
    private bool loading = true;
    private bool saving;
    private bool canConfigure = true;
    private string? error;
    private string? newSecret;
    private SmtpSubmissionPolicyView? policy;
    private SmtpSubmissionMode selectedMode;

    [Parameter]
    public Guid DomainId { get; set; }

    [Parameter]
    public Guid SubdomainId { get; set; }

    private bool IsSubdomain => this.SubdomainId != Guid.Empty;

    private bool HasPendingChange => this.policy is not null && this.selectedMode != this.policy.Mode;

    private bool CanRotate => this.policy?.Mode == SmtpSubmissionMode.RequireAuthentication;

    private string Endpoint => this.IsSubdomain
        ? $"api/email-inbox/subdomains/{this.SubdomainId}/smtp-submission"
        : $"api/email-inbox/domains/{this.DomainId}/smtp-submission";

    private string BackUrl => this.IsSubdomain
        ? $"/admin/subdomains/{this.SubdomainId}"
        : $"/admin/domains/{this.DomainId}";

    protected override async Task OnParametersSetAsync()
    {
        this.loading = true;
        this.canConfigure = true;
        this.error = null;
        this.newSecret = null;
        using var response = await httpClient.GetAsync(this.Endpoint);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            this.canConfigure = false;
            this.error = "You do not have permission to manage SMTP submission for this domain.";
        }
        else if (response.IsSuccessStatusCode)
        {
            this.policy = await response.Content.ReadFromJsonAsync<SmtpSubmissionPolicyView>();
            this.selectedMode = this.policy?.Mode ?? SmtpSubmissionMode.Disabled;
        }
        else
        {
            this.error = "SMTP submission settings could not be loaded.";
        }

        this.loading = false;
    }

    private async Task SaveAsync()
    {
        if (!this.HasPendingChange)
        {
            return;
        }

        await this.ApplyAsync();
    }

    private async Task RotateAsync()
    {
        if (!this.CanRotate || this.HasPendingChange)
        {
            return;
        }

        await this.ApplyAsync();
    }

    private async Task CopyAsync(string value)
    {
        await jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", value);
    }

    private async Task ApplyAsync()
    {
        this.saving = true;
        this.error = null;
        this.newSecret = null;
        using var response = await httpClient.PutAsJsonAsync(
            this.Endpoint, new SmtpSubmissionPolicyRequest(this.selectedMode));
        if (response.IsSuccessStatusCode)
        {
            this.policy = await response.Content.ReadFromJsonAsync<SmtpSubmissionPolicyView>();
            this.newSecret = this.policy?.Secret;
        }
        else
        {
            this.error = response.StatusCode == HttpStatusCode.BadRequest
                ? "Choose a valid authentication setting."
                : "SMTP submission settings could not be saved.";
        }

        this.saving = false;
    }
}
