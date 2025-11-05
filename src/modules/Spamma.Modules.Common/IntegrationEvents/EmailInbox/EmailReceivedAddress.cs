namespace Spamma.Modules.Common.IntegrationEvents.EmailInbox;

/// <summary>
/// DTO for email address with name and type.
/// </summary>
public record EmailReceivedAddress(string Address, string? DisplayName, int Type);
