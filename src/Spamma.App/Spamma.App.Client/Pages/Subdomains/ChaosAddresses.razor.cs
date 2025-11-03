using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.Common;
using Spamma.Modules.DomainManagement.Client.Application.Commands.CreateChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.DisableChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.EnableChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Subdomains;

public partial class ChaosAddresses(IQuerier querier,
    ICommander commander,
    INotificationService notificationService,
    AuthenticationStateProvider authenticationStateProvider)
{
    private bool isLoading = true;

    private IReadOnlyList<ChaosAddressSummary>? chaosAddresses;

    private bool isInitialized;
    private string? errorMessage;

    private ChaosAddressSearchRequest searchRequest = new();
    private Dictionary<string, List<SubdomainOption>>? subdomainsByDomain;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(
            LoadChaosAddresses(),
            LoadSubdomains());
        isInitialized = true;
    }

    private async Task LoadSubdomains()
    {
        var query = new SearchSubdomainsQuery(null, null, null, 1, 1000, "SubdomainName", false);
        var result = await querier.Send(query);
        if (result.Status == QueryResultStatus.Succeeded && result.Data.TotalCount > 0)
        {
            subdomainsByDomain = result.Data.Items
                .GroupBy(x => x.ParentDomainName)
                .OrderBy(x => x.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .Select(x => new SubdomainOption { Id = x.Id, SubdomainName = x.SubdomainName })
                        .OrderBy(x => x.SubdomainName)
                        .ToList());
        }
    }

    private async Task LoadChaosAddresses()
    {
        isLoading = true;
        StateHasChanged();
        var enabled = searchRequest.EnabledFilter switch
        {
            "enabled" => true,
            "disabled" => false,
            _ => (bool?)null
        };

        var subdomainId = string.IsNullOrEmpty(searchRequest.SubdomainIdFilter)
            ? (Guid?)null
            : Guid.Parse(searchRequest.SubdomainIdFilter);

        var query = new SearchChaosAddressesQuery(
            searchRequest.SearchTerm,
            subdomainId,
            enabled,
            1,
            50,
            searchRequest.SortBy,
            true);

        var result = await querier.Send(query);
        chaosAddresses = result is { Status: QueryResultStatus.Succeeded, Data.TotalCount: > 0 } ? result.Data.Items : new List<ChaosAddressSummary>();


        isLoading = false;

    }

    private async Task HandleCreated()
    {
        await LoadChaosAddresses();
    }

    private void ToggleChaosAddress(Guid id, bool enable)
    {
        var chaos = chaosAddresses?.FirstOrDefault(x => x.Id == id);
        if (chaos != null)
        {
            if (enable)
            {
                enableConfirmData = chaos;
                showEnableConfirm = true;
                enableIdToProcess = id;
            }
            else
            {
                suspendConfirmData = chaos;
                showSuspendConfirm = true;
                suspendIdToProcess = id;
            }
        }
    }

    private async Task ConfirmDisable()
    {
        isSuspendProcessing = true;
        StateHasChanged();

        var command = new DisableChaosAddressCommand(suspendIdToProcess, Guid.NewGuid());
        var result = await commander.Send(command);

        if (result.Status == CommandResultStatus.Succeeded)
        {
            CloseSuspendConfirm();
            await LoadChaosAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to disable chaos address.");
        }

        isSuspendProcessing = false;
        StateHasChanged();
    }

    private async Task ConfirmEnable()
    {
        isEnableProcessing = true;
        StateHasChanged();

        var command = new EnableChaosAddressCommand(enableIdToProcess, Guid.NewGuid());
        var result = await commander.Send(command);

        if (result.Status == CommandResultStatus.Succeeded)
        {
            CloseEnableConfirm();
            await LoadChaosAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to enable chaos address.");
        }

        isEnableProcessing = false;
        StateHasChanged();
    }

    private void DeleteChaosAddress(Guid id)
    {
        var chaos = chaosAddresses?.FirstOrDefault(x => x.Id == id);
        if (chaos != null)
        {
            deleteConfirmData = chaos;
            showDeleteConfirm = true;
            deleteIdToProcess = id;
        }
    }

    private async Task ConfirmDelete()
    {
        isDeleteProcessing = true;
        StateHasChanged();

        // TODO: Implement DeleteChaosAddressCommand
        // For now, just close the dialog
        await Task.Delay(1000); // Simulate processing

        CloseDeleteConfirm();
        await LoadChaosAddresses();

        isDeleteProcessing = false;
        StateHasChanged();
    }

    private class ChaosAddressSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string SubdomainIdFilter { get; set; } = "";
        public string EnabledFilter { get; set; } = "";
        public string SortBy { get; set; } = "CreatedAt";
    }

    private class SubdomainOption
    {
        public Guid Id { get; set; }
        public string SubdomainName { get; set; } = string.Empty;
    }
    
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

    private void OpenCreate()
    {
        showModal = true;
        model = new();
    }

    public void CloseCreate()
    {
        showModal = false;
        model = new();
        errorMessage = null;
    }

    private void CloseSuspendConfirm()
    {
        showSuspendConfirm = false;
        suspendConfirmData = null;
        suspendIdToProcess = Guid.Empty;
    }

    private void CloseEnableConfirm()
    {
        showEnableConfirm = false;
        enableConfirmData = null;
        enableIdToProcess = Guid.Empty;
    }

    private void CloseDeleteConfirm()
    {
        showDeleteConfirm = false;
        deleteConfirmData = null;
        deleteIdToProcess = Guid.Empty;
    }

    private async Task HandleCreate()
    {
        isCreating = true;
        StateHasChanged();

            var command = new CreateChaosAddressCommand(
                Guid.NewGuid(), // Id
                Guid.Empty, // DomainId (to be set by backend)
                model.SubdomainId, // SubdomainId
                model.LocalPart,
                (SmtpResponseCode)model.SmtpCode,
                Guid.Empty // CreatedBy (to be set by backend from auth context)
            );

            var result = await commander.Send(command);

            if (result.Status == CommandResultStatus.Succeeded)
            {
                CloseCreate();
                await LoadChaosAddresses();
            }
            else
            {
                notificationService.ShowError("Failed to create chaos address");
            }
       
            isCreating = false;
            StateHasChanged();
        
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
            _ => "Unknown"
        };
    }

    public class CreateChaosAddressFormModel
    {
        public string LocalPart { get; set; } = string.Empty;
        public int SmtpCode { get; set; }
        public string? Description { get; set; }
        public Guid SubdomainId { get; set; }

        public string SubdomainIdString
        {
            get => SubdomainId == Guid.Empty ? "" : SubdomainId.ToString();
            set => SubdomainId = string.IsNullOrEmpty(value) ? Guid.Empty : Guid.Parse(value);
        }
    }
}

