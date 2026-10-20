namespace Spamma.Modules.DomainManagement.Infrastructure.Dns;

internal record DnsRecordSpec(DnsRecordType Type, string Name, string Content, int Ttl, int? Priority = null);
