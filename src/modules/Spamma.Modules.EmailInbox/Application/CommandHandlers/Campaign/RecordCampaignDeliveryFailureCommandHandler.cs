using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers.Campaign;

internal class RecordCampaignDeliveryFailureCommandHandler(
    IEnumerable<IValidator<RecordCampaignDeliveryFailureCommand>> validators,
    ILogger<RecordCampaignDeliveryFailureCommandHandler> logger,
    ICampaignRepository campaignRepository)
    : CommandHandler<RecordCampaignDeliveryFailureCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(RecordCampaignDeliveryFailureCommand request, CancellationToken cancellationToken)
    {
        if (request.DomainId == Guid.Empty || request.SubdomainId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.CampaignValue) || request.CampaignValue.Length > 255 ||
            request.FailureId == Guid.Empty || request.AttemptId == Guid.Empty || request.SmtpCode is < 400 or > 599)
        {
            return CommandResult.Failed(new BluQubeErrorData(EmailInboxErrorCodes.InvalidCampaignData));
        }

        var campaignId = CampaignIdentity.FromValue(request.SubdomainId, request.CampaignValue);
        var existing = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);
        var campaign = existing.HasValue
            ? existing.Value
            : Domain.CampaignAggregate.Campaign.ObserveFailure(
                campaignId, request.DomainId, request.SubdomainId, request.CampaignValue, request.ObservedAt);

        var recorded = campaign.RecordDeliveryFailure(request.FailureId, request.AttemptId, request.SmtpCode, request.ObservedAt);
        if (recorded.IsFailure)
        {
            return CommandResult.Failed(recorded.Error);
        }

        var saved = await campaignRepository.SaveAsync(campaign, cancellationToken);
        return saved.IsSuccess
            ? CommandResult.Succeeded()
            : CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
    }
}
