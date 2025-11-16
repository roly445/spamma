using Microsoft.AspNetCore.Authentication;

namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

#pragma warning disable S2094 // Remove this empty class, write its code or make it an "interface".
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = "X-API-Key";

    public string QueryParameterName { get; set; } = "api_key";

    public bool AllowQueryParameter { get; set; } = true;
}
#pragma warning restore S2094 // Remove this empty class, write its code or make it an "interface".