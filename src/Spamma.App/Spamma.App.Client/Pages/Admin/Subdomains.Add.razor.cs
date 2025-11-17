using Spamma.App.Client.Components.UserControls;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Code-behind for the Subdomains page.
/// </summary>
public partial class Subdomains
{
    private Task HandleSubdomainCreated()
    {
        return this.LoadSubdomains();
    }

    private void OpenAddSubdomainModal()
    {
        if (this.addSubdomain == null)
        {
            return;
        }

        this.addSubdomain.Open(new AddSubdomain.DataModel(null, null, this.availableDomains));
    }
}