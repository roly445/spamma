using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;

internal class SetupDomainDnsCommandHandler(
    IDomainRepository repository,
    DnsSetupService dnsSetup,
    IEnumerable<IValidator<SetupDomainDnsCommand>> validators,
    ILogger<SetupDomainDnsCommandHandler> logger)
    : CommandHandler<SetupDomainDnsCommand, SetupDomainDnsCommandResult>(validators, logger)
{
    protected override async Task<CommandResult<SetupDomainDnsCommandResult>> HandleInternal(SetupDomainDnsCommand request, CancellationToken cancellationToken)
    {
        var domainMaybe = await repository.GetByIdAsync(request.DomainId, cancellationToken);
        if (domainMaybe.HasNoValue)
        {
            return CommandResult<SetupDomainDnsCommandResult>.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, "Domain not found."));
        }

        var domain = domainMaybe.Value;
        if (domain.IsSuspended)
        {
            return CommandResult<SetupDomainDnsCommandResult>.Succeeded(new(DnsSetupStatus.Failed, "Suspended domains cannot change DNS records."));
        }

        var record = new DnsRecordSpec(DnsRecordType.Txt, domain.Name, $"spamma-verification={domain.VerificationToken}", 3600);
        var result = await dnsSetup.EnsureAsync(domain.DnsIntegrationId, domain.Name, record, cancellationToken);
        return CommandResult<SetupDomainDnsCommandResult>.Succeeded(new(result.Status, result.Message));
    }
}
