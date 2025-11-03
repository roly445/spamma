using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.Common;
using Spamma.Modules.DomainManagement.Client.Application.Commands.CreateChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.DeleteChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.DisableChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.EditChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.EnableChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Subdomains;

public partial class ChaosAddresses(IQuerier querier,
    ICommander commander,
    INotificationService notificationService,
    IJSRuntime jsRuntime)
{
    private bool isLoading = true;

    private IReadOnlyList<ChaosAddressSummary>? chaosAddresses;

    private bool isInitialized;

    private ChaosAddressSearchRequest searchRequest = new();
    private Dictionary<string, List<SubdomainOption>>? subdomainsByDomain;

    private bool showModal;
    private bool isCreating;

    private CreateChaosAddressFormModel model = new();

    // Suspend/Disable Dialog
    private bool showSuspendConfirm;
    private bool isSuspendProcessing;
    private ChaosAddressSummary? suspendConfirmData;
    private Guid suspendIdToProcess;

    // Enable Dialog
    private bool showEnableConfirm;
    private bool isEnableProcessing;
    private ChaosAddressSummary? enableConfirmData;
    private Guid enableIdToProcess;

    // Delete Dialog
    private bool showDeleteConfirm;
    private bool isDeleteProcessing;
    private ChaosAddressSummary? deleteConfirmData;
    private Guid deleteIdToProcess;

    // Edit Slideout
    private bool showEditSlideout;
    private bool isEditSaving;
    private ChaosAddressSummary? editChaosAddress;
    private string editDescription = string.Empty;
    private string editLocalPart = string.Empty;
    private string editSmtpCodeString = string.Empty;
    private string editSubdomainIdString = string.Empty;
    private string? outsideClickHandlerId;
    private DotNetObjectReference<ChaosAddresses>? dotNetRef;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(
            this.LoadChaosAddresses(),
            this.LoadSubdomains());
        this.isInitialized = true;
    }

    private static IEnumerable<SmtpResponseCode> GetSmtpCodes()
    {
        // Return common chaos codes
        return new[]
        {
            SmtpResponseCode.ServiceNotAvailable, // 421
            SmtpResponseCode.MailboxUnavailable, // 450
            SmtpResponseCode.RequestedActionAborted, // 451
            SmtpResponseCode.InsufficientSystemStorage, // 452
            SmtpResponseCode.SyntaxError, // 500
            SmtpResponseCode.MailboxUnavailablePermanent, // 550
            SmtpResponseCode.UserNotLocalTryForwardPath, // 551
            SmtpResponseCode.ExceededStorageAllocation, // 552
            SmtpResponseCode.MailboxNameNotAllowed, // 553
        };
    }

    private static string GetSmtpDescription(SmtpResponseCode smtpCode)
    {
        return smtpCode switch
        {
            SmtpResponseCode.ServiceNotAvailable => "Service Not Available",
            SmtpResponseCode.MailboxUnavailable => "Mailbox Unavailable",
            SmtpResponseCode.RequestedActionAborted => "Requested Action Aborted",
            SmtpResponseCode.InsufficientSystemStorage => "Insufficient Storage",
            SmtpResponseCode.SyntaxError => "Syntax Error",
            SmtpResponseCode.MailboxUnavailablePermanent => "Mailbox Unavailable (Permanent)",
            SmtpResponseCode.UserNotLocalTryForwardPath => "User Not Local",
            SmtpResponseCode.ExceededStorageAllocation => "Exceeded Storage",
            SmtpResponseCode.MailboxNameNotAllowed => "Mailbox Name Not Allowed",
            _ => "Unknown",
        };
    }

    private async Task LoadSubdomains()
    {
        var query = new SearchSubdomainsQuery(null, null, null, 1, 1000, "SubdomainName", false);
        var result = await querier.Send(query);
        if (result.Status == QueryResultStatus.Succeeded && result.Data.TotalCount > 0)
        {
            this.subdomainsByDomain = result.Data.Items
                .GroupBy(x => x.ParentDomainName)
                .OrderBy(x => x.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .Select(x => new SubdomainOption { Id = x.Id, DomainId = x.ParentDomainId, SubdomainName = x.SubdomainName })
                        .OrderBy(x => x.SubdomainName)
                        .ToList());
        }
    }

    private async Task LoadChaosAddresses()
    {
        this.isLoading = true;
        this.StateHasChanged();
        var enabled = this.searchRequest.EnabledFilter switch
        {
            "enabled" => true,
            "disabled" => false,
            _ => (bool?)null,
        };

        var subdomainId = string.IsNullOrEmpty(this.searchRequest.SubdomainIdFilter)
            ? (Guid?)null
            : Guid.Parse(this.searchRequest.SubdomainIdFilter);

        var query = new SearchChaosAddressesQuery(
            this.searchRequest.SearchTerm,
            subdomainId,
            enabled,
            1,
            50,
            this.searchRequest.SortBy,
            true);

        var result = await querier.Send(query);
        this.chaosAddresses = result is { Status: QueryResultStatus.Succeeded, Data.TotalCount: > 0 } ? result.Data.Items : new List<ChaosAddressSummary>();

        this.isLoading = false;
    }

    private void ToggleChaosAddress(Guid id, bool enable)
    {
        var chaos = this.chaosAddresses?.FirstOrDefault(x => x.Id == id);
        if (chaos != null)
        {
            if (enable)
            {
                this.enableConfirmData = chaos;
                this.showEnableConfirm = true;
                this.enableIdToProcess = id;
            }
            else
            {
                this.suspendConfirmData = chaos;
                this.showSuspendConfirm = true;
                this.suspendIdToProcess = id;
            }
        }
    }

    private async Task ConfirmDisable()
    {
        this.isSuspendProcessing = true;
        this.StateHasChanged();

        var command = new DisableChaosAddressCommand(this.suspendIdToProcess, Guid.NewGuid());
        var result = await commander.Send(command);

        if (result.Status == CommandResultStatus.Succeeded)
        {
            this.CloseSuspendConfirm();
            await this.LoadChaosAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to disable chaos address.");
        }

        this.isSuspendProcessing = false;
        this.StateHasChanged();
    }

    private async Task ConfirmEnable()
    {
        this.isEnableProcessing = true;
        this.StateHasChanged();

        var command = new EnableChaosAddressCommand(this.enableIdToProcess, Guid.NewGuid());
        var result = await commander.Send(command);

        if (result.Status == CommandResultStatus.Succeeded)
        {
            this.CloseEnableConfirm();
            await this.LoadChaosAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to enable chaos address.");
        }

        this.isEnableProcessing = false;
        this.StateHasChanged();
    }

    private void DeleteChaosAddress(Guid id)
    {
        var chaos = this.chaosAddresses?.FirstOrDefault(x => x.Id == id);
        if (chaos != null)
        {
            this.deleteConfirmData = chaos;
            this.showDeleteConfirm = true;
            this.deleteIdToProcess = id;
        }
    }

    private async Task ConfirmDelete()
    {
        this.isDeleteProcessing = true;
        this.StateHasChanged();

        var command = new DeleteChaosAddressCommand(this.deleteIdToProcess);
        var result = await commander.Send(command);

        if (result.Status == CommandResultStatus.Succeeded)
        {
            this.CloseDeleteConfirm();
            await this.LoadChaosAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to delete chaos address");
        }

        this.isDeleteProcessing = false;
        this.StateHasChanged();
    }

    private void OpenCreate()
    {
        this.showModal = true;
        this.model = new();
    }

    private void CloseCreate()
    {
        this.showModal = false;
        this.model = new();
    }

    private void CloseSuspendConfirm()
    {
        this.showSuspendConfirm = false;
        this.suspendConfirmData = null;
        this.suspendIdToProcess = Guid.Empty;
    }

    private void CloseEnableConfirm()
    {
        this.showEnableConfirm = false;
        this.enableConfirmData = null;
        this.enableIdToProcess = Guid.Empty;
    }

    private void CloseDeleteConfirm()
    {
        this.showDeleteConfirm = false;
        this.deleteConfirmData = null;
        this.deleteIdToProcess = Guid.Empty;
    }

    private void OpenEdit(ChaosAddressSummary chaos)
    {
        this.editChaosAddress = chaos;
        this.editDescription = string.Empty;
        this.editLocalPart = chaos.LocalPart;
        this.editSmtpCodeString = ((int)chaos.ConfiguredSmtpCode).ToString();
        this.editSubdomainIdString = chaos.SubdomainId == Guid.Empty ? string.Empty : chaos.SubdomainId.ToString();
        this.showEditSlideout = true;
        try
        {
            this.dotNetRef = DotNetObjectReference.Create(this);
            _ = jsRuntime.InvokeAsync<string>("registerOutsideClickHandler", "#chaos-edit-panel", this.dotNetRef, "CloseEditSlideout")
                .AsTask().ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        this.outsideClickHandlerId = t.Result;
                    }
                });
        }
        catch
        {
            // swallow - non-fatal if JS not available
        }
    }

    private void CloseEditSlideout()
    {
        this.showEditSlideout = false;
        this.editChaosAddress = null;
        this.editDescription = string.Empty;
        this.editLocalPart = string.Empty;
        this.editSmtpCodeString = string.Empty;
        this.editSubdomainIdString = string.Empty;
        try
        {
            if (!string.IsNullOrEmpty(this.outsideClickHandlerId))
            {
                _ = jsRuntime.InvokeVoidAsync("removeOutsideClickHandler", this.outsideClickHandlerId);
                this.outsideClickHandlerId = null;
            }

            this.dotNetRef?.Dispose();
            this.dotNetRef = null;
        }
        catch
        {
            // ignore
        }
    }

    private async Task SaveEditChanges()
    {
        this.isEditSaving = true;
        this.StateHasChanged();

        if (this.editChaosAddress == null)
        {
            this.isEditSaving = false;
            return;
        }

        if (!int.TryParse(this.editSmtpCodeString, out var smtpCodeValue))
        {
            notificationService.ShowError("Invalid SMTP code selected");
            this.isEditSaving = false;
            this.StateHasChanged();
            return;
        }

        // determine selected subdomain and domain id
        Guid selectedSubdomainId = string.IsNullOrEmpty(this.editSubdomainIdString) ? this.editChaosAddress.SubdomainId : Guid.Parse(this.editSubdomainIdString);
        var selectedSubdomain = this.subdomainsByDomain?.SelectMany(x => x.Value).FirstOrDefault(x => x.Id == selectedSubdomainId);
        Guid domainId = selectedSubdomain?.DomainId ?? this.editChaosAddress.DomainId;

        var command = new EditChaosAddressCommand(
            this.editChaosAddress.Id,
            domainId,
            selectedSubdomainId,
            this.editLocalPart,
            (SmtpResponseCode)smtpCodeValue,
            string.IsNullOrWhiteSpace(this.editDescription) ? null : this.editDescription);

        var result = await commander.Send(command);

        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess("Chaos address updated successfully");
            this.CloseEditSlideout();
            await this.LoadChaosAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to update chaos address");
        }

        this.isEditSaving = false;
        this.StateHasChanged();
    }

    private async Task HandleCreate()
    {
        this.isCreating = true;
        this.StateHasChanged();

        var selectedSubdomain = this.subdomainsByDomain?.SelectMany(x => x.Value).FirstOrDefault(x => x.Id == this.model.SubdomainId);
        var domainId = selectedSubdomain?.DomainId ?? Guid.Empty;

        var command = new CreateChaosAddressCommand(
            Guid.NewGuid(), // Id
            domainId, // DomainId (looked up from subdomain)
            this.model.SubdomainId, // SubdomainId
            this.model.LocalPart,
            (SmtpResponseCode)this.model.SmtpCode,
            Guid.Empty); // CreatedBy (to be set by backend from auth context)

        var result = await commander.Send(command);

        if (result.Status == CommandResultStatus.Succeeded)
        {
            this.CloseCreate();
            await this.LoadChaosAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to create chaos address");
        }

        this.isCreating = false;
        this.StateHasChanged();
    }

    private string GetSubdomainDisplayName(Guid subdomainId)
    {
        if (this.subdomainsByDomain == null)
        {
            return string.Empty;
        }

        foreach (var domainGroup in this.subdomainsByDomain)
        {
            var subdomain = domainGroup.Value.FirstOrDefault(s => s.Id == subdomainId);
            if (subdomain != null)
            {
                return $"@{subdomain.SubdomainName}.{domainGroup.Key}";
            }
        }

        return string.Empty;
    }

    public class CreateChaosAddressFormModel
    {
        public string LocalPart { get; set; } = string.Empty;

        public int SmtpCode { get; set; }

        public string? Description { get; set; }

        public Guid SubdomainId { get; set; }

        public string SubdomainIdString
        {
            get => this.SubdomainId == Guid.Empty ? string.Empty : this.SubdomainId.ToString();
            set => this.SubdomainId = string.IsNullOrEmpty(value) ? Guid.Empty : Guid.Parse(value);
        }
    }

    private sealed class ChaosAddressSearchRequest
    {
        public string? SearchTerm { get; set; }

        public string SubdomainIdFilter { get; set; } = string.Empty;

        public string EnabledFilter { get; set; } = string.Empty;

        public string SortBy { get; set; } = "CreatedAt";
    }

    private sealed class SubdomainOption
    {
        public Guid Id { get; set; }

        public Guid DomainId { get; set; }

        public string SubdomainName { get; set; } = string.Empty;
    }
}

