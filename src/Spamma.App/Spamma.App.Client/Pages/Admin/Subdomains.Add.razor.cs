using Spamma.App.Client.Components.UserControls;

namespace Spamma.App.Client.Pages.Admin;

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