namespace Spamma.Modules.UserManagement.Client.Contracts;

public enum AccountSuspensionReason
{
    Unknown = 0,
    PolicyViolation,
    SpamAbuse,
    SecurityConcern,
    RequestedByUser,
    Administrative,
    Other,
}