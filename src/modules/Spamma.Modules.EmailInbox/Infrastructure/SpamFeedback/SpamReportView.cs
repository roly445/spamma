namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed record SpamReportView(
    Guid ReportId,
    SpamReportTrigger Trigger,
    DateTimeOffset CreatedAt,
    FeedbackChannelState Email,
    FeedbackChannelState Webhook)
{
    public static SpamReportView From(SpamReport report) => new(
        report.Id, report.Trigger, report.CreatedAt, report.EmailDelivery, report.WebhookDelivery);
}
