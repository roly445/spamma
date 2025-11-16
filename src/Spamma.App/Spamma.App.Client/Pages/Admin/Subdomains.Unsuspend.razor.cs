using Spamma.App.Client.Components.UserControls.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Admin;

public partial class Subdomains
{
    private UnsuspendSubdomain? _unsuspendSubdomain;

    private Task HandleSubdomainUnsuspended()
    {
        return this.LoadSubdomains();
    }

    private void OpenUnsuspendSubdomain(SearchSubdomainsQueryResult.SubdomainSummary subdomain)
    {
        if (this._unsuspendSubdomain == null)
        {
            return;
        }

        this._unsuspendSubdomain.Open(new UnsuspendSubdomain.DataModel(subdomain.SubdomainId, subdomain.FullDomainName, subdomain.CreatedAt));
    }
}