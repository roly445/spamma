namespace Spamma.Modules.Common.Client;

public enum SmtpResponseCode
{
    ServiceNotAvailable = 421,
    MailboxUnavailable = 450,
    RequestedActionAborted = 451,
    InsufficientSystemStorage = 452,
    SyntaxError = 500,
    MailboxUnavailablePermanent = 550,
    UserNotLocalTryForwardPath = 551,
    ExceededStorageAllocation = 552,
    MailboxNameNotAllowed = 553,
}