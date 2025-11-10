namespace Spamma.Modules.EmailInbox.Client.Application;

/// <summary>
/// Filter type for push integration email matching.
/// </summary>
public enum FilterType
{
    /// <summary>
    /// Match all emails in the subdomain.
    /// </summary>
    AllEmails,

    /// <summary>
    /// Match emails to a specific email address.
    /// </summary>
    SingleEmail,

    /// <summary>
    /// Match emails using a regex pattern.
    /// </summary>
    Regex,
}