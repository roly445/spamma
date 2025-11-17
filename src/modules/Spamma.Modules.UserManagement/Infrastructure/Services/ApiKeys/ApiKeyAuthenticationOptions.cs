using Microsoft.AspNetCore.Authentication;

namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = "X-API-Key";

    public string QueryParameterName { get; set; } = "api_key";

    public bool AllowQueryParameter { get; set; } = true;
}