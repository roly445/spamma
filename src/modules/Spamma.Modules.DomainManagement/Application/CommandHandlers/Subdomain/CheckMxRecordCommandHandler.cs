using BluQube.Commands;
using DnsClient;
using FluentValidation;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Subdomain;

public class CheckMxRecordCommandHandler(ISubdomainRepository repository, IDocumentSession documentSession, IOptions<Settings> settings, TimeProvider timeProvider, ILookupClient lookupClient, IEnumerable<IValidator<CheckMxRecordCommand>> validators, ILogger<CheckMxRecordCommandHandler> logger)
    : CommandHandler<CheckMxRecordCommand, CheckMxRecordCommandResult>(validators, logger)
{
    protected override async Task<CommandResult<CheckMxRecordCommandResult>> HandleInternal(CheckMxRecordCommand request, CancellationToken cancellationToken)
    {
        var subdomainMaybe = await repository.GetByIdAsync(request.SubdomainId, cancellationToken);
        if (subdomainMaybe.HasNoValue)
        {
            return CommandResult<CheckMxRecordCommandResult>.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Subdomain with ID {request.SubdomainId} not found"));
        }

        var lookup = await documentSession.Query<SubdomainLookup>()
            .FirstOrDefaultAsync(x => x.Id == request.SubdomainId, token: cancellationToken);
        if (lookup == null)
        {
            return CommandResult<CheckMxRecordCommandResult>.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Subdomain with ID {request.SubdomainId} not found"));
        }

        var isValid = await this.CheckMxRecord(lookup.FullName, cancellationToken);

        var subdomain = subdomainMaybe.Value;
        var whenHappened = timeProvider.GetUtcNow().UtcDateTime;
        var status = isValid ? MxStatus.Valid : MxStatus.Invalid;

        var logResult = subdomain.LogMxRecordCheck(status, whenHappened);
        if (logResult.IsFailure)
        {
            return CommandResult<CheckMxRecordCommandResult>.Failed(logResult.Error);
        }

        var saveResult = await repository.SaveAsync(subdomain, cancellationToken);
        return !saveResult.IsSuccess ?
            CommandResult<CheckMxRecordCommandResult>.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) :
            CommandResult<CheckMxRecordCommandResult>.Succeeded(new CheckMxRecordCommandResult(status, whenHappened));
    }

    private async Task<bool> CheckMxRecord(string domain, CancellationToken cancellationToken)
    {
        try
        {
            var result = await lookupClient.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);

            if (result.HasError || !result.Answers.Any() || !result.Answers.MxRecords().Any() || result.Answers.MxRecords().Count() > 1)
            {
                return false;
            }

            var mxRecord = result.Answers.MxRecords().First();

            return mxRecord.Exchange.Value.Contains(settings.Value.MailServerHostname, StringComparison.InvariantCultureIgnoreCase);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DNS query failed: {ex.Message}");
            return false;
        }
    }
}