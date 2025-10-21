﻿using Spamma.App.Client.Components.UserControls.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the subdomains management page - suspend subdomain functionality.
/// </summary>
public partial class Subdomains
{
    private SuspendSubdomain? _suspendSubdomain;

    private void OpenSuspendSubdomain(SearchSubdomainsQueryResult.SubdomainSummary subdomain)
    {
        if (this._suspendSubdomain == null)
        {
            return;
        }

        this._suspendSubdomain.Open(new SuspendSubdomain.DataModel(subdomain.Id, subdomain.FullDomainName));
    }

    private Task HandleSubdomainSuspended()
    {
        return this.LoadSubdomains();
    }
}