namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

internal sealed record DefaultEmailInboxSettings(bool CatchAllModeEnabled) : IEmailInboxSettings;
