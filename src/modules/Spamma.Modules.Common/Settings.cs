namespace Spamma.Modules.Common;

public class Settings
{
    public string SigningKeyBase64 { get; init; } = string.Empty;

    public string BaseUri { get; init; } = string.Empty;

    public string LoginUri => string.Concat(this.BaseUri.TrimEnd('/'), "/logging-in?token={0}");

    public int AuthenticationTimeInMinutes { get; init; } = 15;

    public Guid PrimaryUserId { get; init; } = Guid.Empty;

    public string MailServerHostname { get; init; } = string.Empty;

    public int MxPriority { get; init; } = 10;
}