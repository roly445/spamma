namespace Spamma.App.Infrastructure.Contracts;

/// <summary>
/// Contains the WebAuthn assertion data including credential ID and response.
/// </summary>
public record AssertionData(
    string Id,
    string RawId,
    AssertionResponseData Response,
    string Type);