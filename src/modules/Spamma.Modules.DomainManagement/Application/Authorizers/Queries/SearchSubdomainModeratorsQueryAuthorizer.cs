﻿using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

public class SearchSubdomainModeratorsQueryAuthorizer : AbstractRequestAuthorizer<SearchSubdomainModeratorsQuery>
{
    public override void BuildPolicy(SearchSubdomainModeratorsQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeModeratorToSubdomainRequirement
        {
            SubdomainId = request.SubdomainId,
        });
    }
}