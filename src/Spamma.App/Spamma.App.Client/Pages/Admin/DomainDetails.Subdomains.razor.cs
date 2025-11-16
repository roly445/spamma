using Spamma.App.Client.Components.UserControls;

namespace Spamma.App.Client.Pages.Admin;

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
        if (this.domain == null || this._addSubdomain == null)
        {
            return;
        }

        this._addSubdomain?.Open(new AddSubdomain.DataModel(this.domain.DomainId, this.domain.DomainName, null));
    }
}