using Spamma.App.Client.Components.UserControls.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the subdomains management page - edit subdomain functionality.
/// </summary>
public partial class Subdomains
{
    private EditSubdomain? _editSubdomain;

    private Task HandleSubdomainUpdated()
    {
        return this.LoadSubdomains();
    }

    private void OpenEditSubdomain(SearchSubdomainsQueryResult.SubdomainSummary subdomain)
    {
        if (this._editSubdomain == null)
        {
            return;
        }

        this._editSubdomain.Open(new EditSubdomain.DataModel(
            subdomain.Id,
            subdomain.Description,
            subdomain.SubdomainName,
            subdomain.SubdomainName,
            subdomain.CreatedAt));
    }
}