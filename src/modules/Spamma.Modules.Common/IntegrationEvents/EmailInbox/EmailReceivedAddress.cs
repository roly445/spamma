namespace Spamma.Modules.Common.IntegrationEvents.EmailInbox;

public record EmailReceivedAddress(string Address, string? DisplayName, int Type);