using BluQube.Constants;
using Spamma.App.Client.Components.UserControls.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the domain details page - changing domain functionality.
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

        this._unsuspendDomain.Open(new UnsuspendDomain.DataModel(this.domain.Id, this.domain.DomainName));
    }

    private void OpenSuspendDomain()
    {
        if (this._suspendDomain == null || this.domain == null)
        {
            return;
        }

        this._suspendDomain.Open(new SuspendDomain.DataModel(this.domain.Id, this.domain.DomainName));
    }

    private Task HandleSubdomainSuspended()
    {
        return this.LoadAsync();
    }

    private void OpenEditPanel()
    {
        if (this._editDomain == null || this.domain == null)
        {
            return;
        }

        this._editDomain.Open(new EditDomain.DataModel(
            this.domain.Id,
            this.domain.DomainName,
            this.domain.PrimaryContact,
            this.domain.Description,
            this.domain.CreatedAt,
            this.domain.IsVerified,
            this.domain.VerifiedAt,
            this.domain.SubdomainCount,
            this.domain.AssignedUserCount));
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
            var result = await commander.Send(new VerifyDomainCommand(this.domain.Id));

            if (result.Status == CommandResultStatus.Succeeded)
            {
                await this.LoadAsync();
            }
        }

        this._isCheckingVerification = false;
        this.StateHasChanged();
    }
}