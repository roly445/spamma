namespace Spamma.Modules.Common;

// Shared enum used by client-facing DTOs. Placed in Common.Client so client assemblies
// (WASM) expose the type to both client and server consumers.
public enum SmtpResponseCode
{
    // 4xx - Temporary failures
    ServiceNotAvailable = 421,
    MailboxUnavailable = 450,
    RequestedActionAborted = 451,
    InsufficientSystemStorage = 452,

    // 5xx - Permanent failures
    SyntaxError = 500,
    MailboxUnavailablePermanent = 550,
    UserNotLocalTryForwardPath = 551,
    ExceededStorageAllocation = 552,
    MailboxNameNotAllowed = 553,
}

