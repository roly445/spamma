using BluQube.Commands;
using DnsClient;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Application.CommandHandlers.Domain;

public class VerifyDomainCommandHandler(
    IDomainRepository repository, TimeProvider timeProvider, ILookupClient lookupClient, IEnumerable<IValidator<VerifyDomainCommand>> validators, ILogger<VerifyDomainCommandHandler> logger)
    : CommandHandler<VerifyDomainCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(VerifyDomainCommand request, CancellationToken cancellationToken)
    {
        var domainMaybe = await repository.GetByIdAsync(request.DomainId, cancellationToken);
        if (domainMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.NotFound, $"Domain with ID {request.DomainId} not found"));
        }

        var domain = domainMaybe.Value;

        var result = await this.CheckTxtRecordExists(domain.Name, $"spamma-verification={domain.VerificationToken}", cancellationToken);

        if (!result)
        {
            return CommandResult.Failed(new BluQubeErrorData(DomainManagementErrorCodes.VerificationFailed, "Domain verification failed. The required TXT record was not found."));
        }

        var verifyResult = domain.MarkAsVerified(timeProvider.GetUtcNow().DateTime);
        if (verifyResult.IsFailure)
        {
            return CommandResult.Failed(verifyResult.Error);
        }

        var saveResult = await repository.SaveAsync(domain, cancellationToken);
        return !saveResult.IsSuccess ? CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed)) : CommandResult.Succeeded();
    }

    private async Task<bool> CheckTxtRecordExists(string domain, string expectedValue, CancellationToken cancellationToken)
    {
        try
        {
            var result = await lookupClient.QueryAsync(domain, QueryType.TXT, cancellationToken: cancellationToken);

            if (!result.Answers.Any())
            {
                return false;
            }

            var txtRecords = result.Answers.TxtRecords();

            return txtRecords.Any(txt =>
                    txt.Text.Any(text => text.Contains(expectedValue)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DNS query failed: {ex.Message}");
            return false;
        }
    }
}