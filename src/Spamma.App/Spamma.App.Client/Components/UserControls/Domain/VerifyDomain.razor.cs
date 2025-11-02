using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spamma.App.Client.Infrastructure.Contracts;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.App.Client.Pages.Admin;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Components.UserControls.Domain;

/// <summary>
/// Backing code for the verify domain modal.
/// </summary>
public partial class VerifyDomain(
    ICommander commander,
    IJSRuntime jsRuntime) : ComponentBase
{
    private bool isVisible;
    private DataModel? _dataModel;
    private VerificationStep _verificationStep = VerificationStep.Instructions;
    private string _currentVerificationToken = string.Empty;
    private DateTime? _lastVerificationCheck;
    private bool _isCheckingVerification;

    private enum VerificationStep
    {
        Instructions,
        Success,
        Failed,
    }

    [Parameter]
    public EventCallback OnVerified { get; set; }

    public void Open(DataModel dataModel)
    {
        this._dataModel = dataModel;
        this._verificationStep = VerificationStep.Instructions;
        this._currentVerificationToken = $"spamma-verification={this._dataModel.VerificationToken}";
        this.isVisible = true;
        this.StateHasChanged();
    }

    private void Close()
    {
        this.isVisible = false;
        this._dataModel = null;
        this._verificationStep = VerificationStep.Instructions;
        this._currentVerificationToken = string.Empty;
    }

    private async Task CheckDomainVerification()
    {
        if (this._dataModel == null)
        {
            return;
        }

        this._isCheckingVerification = true;
        this._lastVerificationCheck = DateTime.Now;
        this.StateHasChanged();

        var result = await commander.Send(new VerifyDomainCommand(this._dataModel.Id));

        if (result.Status == CommandResultStatus.Succeeded)
        {
            await this.OnVerified.InvokeAsync();
            this._verificationStep = VerificationStep.Success;
        }
        else
        {
            this._verificationStep = VerificationStep.Failed;
        }

        this._isCheckingVerification = false;
        this.StateHasChanged();
    }

    private async Task CopyToClipboard(string text)
    {
        await jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }

    public record DataModel(Guid Id, string DomainName, string? VerificationToken);
}