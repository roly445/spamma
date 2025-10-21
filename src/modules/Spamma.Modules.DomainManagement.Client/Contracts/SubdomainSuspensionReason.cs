namespace Spamma.Modules.DomainManagement.Client.Contracts;

public enum SubdomainSuspensionReason
{
    PolicyViolation,
    SecurityConcern,
    TechnicalIssue,
    MaintenanceMode,
    ResourceExceeded,
    AdminRequest,
}