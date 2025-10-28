namespace Spamma.App.Infrastructure.Contracts;

/// <summary>
/// Contains the response data from the WebAuthn assertion.
/// </summary>
public record AssertionResponseData(
    string ClientDataJson,
    string AuthenticatorData,
    string Signature,
    string? UserHandle = null);