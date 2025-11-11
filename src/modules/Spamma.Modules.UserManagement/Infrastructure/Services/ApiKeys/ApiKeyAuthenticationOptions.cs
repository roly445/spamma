using Microsoft.AspNetCore.Authentication;

namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

#pragma warning disable S2094 // Remove this empty class, write its code or make it an "interface".
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets the header name to look for the API key. Default is "X-API-Key".
    /// </summary>
    public string HeaderName { get; set; } = "X-API-Key";

    /// <summary>
    /// Gets or sets the query parameter name to look for the API key. Default is "api_key".
    /// </summary>
    public string QueryParameterName { get; set; } = "api_key";

    /// <summary>
    /// Gets or sets a value indicating whether to allow API key authentication via query parameters.
    /// Default is true for backward compatibility.
    /// </summary>
    public bool AllowQueryParameter { get; set; } = true;
}
#pragma warning restore S2094 // Remove this empty class, write its code or make it an "interface".