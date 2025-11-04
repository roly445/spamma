using BluQube.Commands;
using FluentValidation;
using Marten;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.CommandHandlers;

public class RecordCampaignCaptureCommandHandler(
    IEnumerable<IValidator<RecordCampaignCaptureCommand>> validators,
    ILogger<RecordCampaignCaptureCommandHandler> logger,
    IDocumentSession session)
    : CommandHandler<RecordCampaignCaptureCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(RecordCampaignCaptureCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var campaignId = GuidFromCampaignValue(request.SubdomainId, request.CampaignValue);

            // Check if campaign already exists
            var existing = await session.Query<CampaignSummary>()
                .FirstOrDefaultAsync(c => c.CampaignId == campaignId && c.SubdomainId == request.SubdomainId, cancellationToken);

            // Emit the event regardless - projection will handle creation or update
            var @event = new CampaignCaptured(
                campaignId,
                request.SubdomainId,
                request.MessageId,
                request.CampaignValue,
                DateTimeOffset.UtcNow);

            session.Events.Append(campaignId, @event);

            // If this is the first capture, also create a sample
            if (existing == null)
            {
                var sample = new CampaignSample
                {
                    CampaignId = campaignId,
                    MessageId = request.MessageId,
                    Subject = request.Subject,
                    From = request.From,
                    To = request.To,
                    ReceivedAt = request.ReceivedAt,
                    StoredAt = DateTimeOffset.UtcNow,
                    ContentPreview = request.Subject, // Simplified - would need actual content in production
                };

                session.Store(sample);
            }

            await session.SaveChangesAsync(cancellationToken);
            return CommandResult.Succeeded();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record campaign capture for campaign {CampaignValue}", request.CampaignValue);
            return CommandResult.Failed(new BluQubeErrorData(CommonErrorCodes.SavingChangesFailed));
        }
    }

    private static Guid GuidFromCampaignValue(Guid subdomainId, string campaignValue)
    {
        // Create deterministic GUID from subdomain ID + campaign value
        var combined = subdomainId.ToString() + ":" + campaignValue;
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
        return new Guid(hash.Take(16).ToArray());
    }
}
