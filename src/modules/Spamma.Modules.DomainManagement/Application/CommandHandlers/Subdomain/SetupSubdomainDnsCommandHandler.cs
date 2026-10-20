using BluQube.Commands;
using FluentValidation;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;

internal class SetupSubdomainDnsCommandHandler(
    IDocumentSession session,
    IOptions<Settings> settings,
    DnsSetupService dnsSetup,
    IEnumerable<IValidator<SetupSubdomainDnsCommand>> validators,
    ILogger<SetupSubdomainDnsCommandHandler> logger)
    : CommandHandler<SetupSubdomainDnsCommand, SetupSubdomainDnsCommandResult>(validators, logger)
{
    protected override async Task<CommandResult<SetupSubdomainDnsCommandResult>> HandleInternal(SetupSubdomainDnsCommand request, CancellationToken cancellationToken)
    {
        var subdomain = await session.LoadAsync<SubdomainLookup>(request.SubdomainId, cancellationToken);
        if (subdomain is null)
        {
            return CommandResult<SetupSubdomainDnsCommandResult>.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, "Subdomain not found."));
        }

        var parent = await session.LoadAsync<DomainLookup>(subdomain.DomainId, cancellationToken);
        if (subdomain.IsSuspended || parent is null || parent.IsSuspended || !parent.IsVerified)
        {
            return CommandResult<SetupSubdomainDnsCommandResult>.Succeeded(new(DnsSetupStatus.Failed, "The subdomain and its parent domain must be active before DNS can be changed."));
        }

        if (string.IsNullOrWhiteSpace(settings.Value.MailServerHostname) || settings.Value.MxPriority is < 1 or > 65535)
        {
            return CommandResult<SetupSubdomainDnsCommandResult>.Succeeded(new(DnsSetupStatus.NotConfigured, "Set a valid mail server hostname and MX priority in application settings first."));
        }

        var record = new DnsRecordSpec(DnsRecordType.Mx, subdomain.FullName, settings.Value.MailServerHostname, 3600, settings.Value.MxPriority);
        var result = await dnsSetup.EnsureAsync(parent.DnsIntegrationId, parent.DomainName, record, cancellationToken);
        return CommandResult<SetupSubdomainDnsCommandResult>.Succeeded(new(result.Status, result.Message));
    }
}
