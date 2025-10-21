using Spamma.App.Client.Components.UserControls;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the domain details page - subdomain functionality.
/// </summary>
public partial class DomainDetails
{
    private Subdomains? _subdomains;
    private AddSubdomain? _addSubdomain;

    private Task HandleSubdomainCreated()
    {
        if (this._subdomains == null)
        {
            return Task.CompletedTask;
        }

        return this._subdomains.Reload();
    }

    private void OpenAddSubdomainModal()
    {
        this._addSubdomain?.Open(new AddSubdomain.DataModel(this.domain?.Id, null));
    }
}