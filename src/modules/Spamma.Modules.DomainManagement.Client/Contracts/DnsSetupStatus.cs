namespace Spamma.Modules.DomainManagement.Client.Contracts;

public enum DnsSetupStatus
{
    Created,
    AlreadyPresent,
    NotConfigured,
    Conflict,
    Failed,
}
