namespace Spamma.App.Infrastructure.Contracts;

/// <summary>
/// Represents the assertion request payload from the client during WebAuthn authentication.
/// </summary>
public record AssertionRequest(AssertionData Assertion);