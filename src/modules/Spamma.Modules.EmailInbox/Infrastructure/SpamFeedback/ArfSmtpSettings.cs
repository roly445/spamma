namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed record ArfSmtpSettings(
    string Host,
    int Port,
    string? Username,
    string? Password,
    string FromEmail,
    string FromName,
    bool UseTls);
