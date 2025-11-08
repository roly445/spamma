namespace Spamma.App.Infrastructure.Contracts;

public record AssertionResponseData(
    string ClientDataJson,
    string AuthenticatorData,
    string Signature,
    string? UserHandle = null);