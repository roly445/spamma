using BluQube.Constants;
using Spamma.App.Client.Components.UserControls.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Code-behind for the DomainDetails page.
/// </summary>
public partial class DomainDetails
{
    private SuspendDomain? _suspendDomain;
    private UnsuspendDomain? _unsuspendDomain;
    private EditDomain? _editDomain;

    private Task HandleDomainUnsuspended()
    {
        return this.LoadAsync();
    }

    private void OpenUnsuspendDomain()
    {
        if (this._unsuspendDomain == null || this.domain == null)
        {
            return;
        }

        this._unsuspendDomain.Open(new UnsuspendDomain.DataModel(this.domain.DomainId, this.domain.DomainName));
    }

    private void OpenSuspendDomain()
    {
        if (this._suspendDomain == null || this.domain == null)
        {
            return;
        }

        this._suspendDomain.Open(new SuspendDomain.DataModel(this.domain.DomainId, this.domain.DomainName));
    }

    private Task HandleSubdomainSuspended()
    {
        return this.LoadAsync();
    }

    private async Task OpenEditPanel()
    {
        if (this._editDomain == null || this.domain == null)
        {
            return;
        }

        await this._editDomain.Open(new EditDomain.DataModel(
            this.domain.DomainId,
            this.domain.DomainName,
            this.domain.PrimaryContact,
            this.domain.Description,
            this.domain.CreatedAt,
            this.domain.IsVerified,
            this.domain.VerifiedAt,
            this.domain.SubdomainCount,
            this.domain.AssignedUserCount,
            this.domain.DnsIntegrationId));
    }

    private Task HandleDomainSaved()
    {
        return this.LoadAsync(); // Refresh to show updated data
    }

    private async Task CheckVerification()
    {
        this._isCheckingVerification = true;
        this.StateHasChanged();

        if (this.domain != null)
        {
            var result = await commander.Send(new VerifyDomainCommand(this.domain.DomainId));

            if (result.Status == CommandResultStatus.Succeeded)
            {
                await this.LoadAsync();
            }
        }

        this._isCheckingVerification = false;
        this.StateHasChanged();
    }

    private async Task SetupDomainDns()
    {
        if (this.domain is null)
        {
            return;
        }

        this._isSettingUpDns = true;
        try
        {
            var result = await commander.Send(new SetupDomainDnsCommand(this.domain.DomainId));
            this._dnsSetupMessage = result.Status == CommandResultStatus.Succeeded
                ? result.Data.Message
                : "DNS setup could not be completed. Add the record manually or retry.";
        }
        finally
        {
            this._isSettingUpDns = false;
        }
    }
}
