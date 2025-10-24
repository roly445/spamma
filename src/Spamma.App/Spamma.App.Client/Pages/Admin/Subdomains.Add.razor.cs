using Spamma.App.Client.Components.UserControls;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the subdomains management page - add subdomain functionality.
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