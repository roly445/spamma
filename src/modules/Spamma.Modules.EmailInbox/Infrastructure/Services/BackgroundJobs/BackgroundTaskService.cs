using BluQube.Commands;
using BluQube.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MimeKit;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public class BackgroundTaskService(
    IBackgroundTaskQueue taskQueue,
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var commander = scope.ServiceProvider.GetRequiredService<ICommander>();
        var hostEnv = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await taskQueue.DequeueAsync(stoppingToken);
            var messageId = Guid.NewGuid();

            try
            {
                workItem.MimeStream.Position = 0;

                var message = await MimeMessage.LoadAsync(workItem.MimeStream, stoppingToken);
                switch (workItem)
                {
                    case CampaignCaptureJob:
                    {
                        var result = await commander.Send(
                            new RecordCampaignCaptureCommand(
                                workItem.DomainId,
                                workItem.SubdomainId,
                                messageId,
                                message.Headers["x-spamma-camp"],
                                message.Date), stoppingToken);

                        if (result is { Status: CommandResultStatus.Succeeded, Data.IsFirstEmail: true })
                        {
                            await ExtractEmailAddressesAndSendCommand(messageId, message, commander, workItem,
                                hostEnv.ContentRootPath, result.Data.CampaignId, cancellationToken: stoppingToken);
                        }

                        break;
                    }

                    case ChaosEmailCaptureJob captureJob:
                        await commander.Send(
                            new RecordChaosAddressReceivedCommand(captureJob.ChaosAddressId, message.Date),
                            stoppingToken);
                        break;
                    case StandardEmailCaptureJob standardJob:
                        await ExtractEmailAddressesAndSendCommand(standardJob.MessageId, message, commander, workItem,
                            hostEnv.ContentRootPath, cancellationToken: stoppingToken);
                        break;
                }
            }
            catch (Exception)
            {
                // Log the exception if necessary
            }
            finally
            {
                await workItem.MimeStream.DisposeAsync();
            }
        }
    }

    private static async Task ExtractEmailAddressesAndSendCommand(
        Guid messageId, MimeMessage message, ICommander commander,
        IBaseEmailCaptureJob workItem, string contentPath, Guid? campaignId = null, CancellationToken cancellationToken = default)
    {
        var addresses = message.To.Mailboxes
            .Select(x => new EmailAddress(x.Address, x.Name, EmailAddressType.To))
            .ToList();
        addresses.AddRange(message.Cc.Mailboxes
            .Select(x => new EmailAddress(x.Address, x.Name, EmailAddressType.Cc)));
        addresses.AddRange(message.Bcc.Mailboxes
            .Select(x => new EmailAddress(x.Address, x.Name, EmailAddressType.Bcc)));
        addresses.AddRange(message.From.Mailboxes
            .Select(x => new EmailAddress(x.Address, x.Name, EmailAddressType.From)));

        if (campaignId == null)
        {
            await commander.Send(
                new ReceivedEmailCommand(
                    messageId,
                    workItem.DomainId,
                    workItem.SubdomainId,
                    message.Subject,
                    message.Date,
                    addresses), cancellationToken);
        }
        else
        {
            await commander.Send(
                new CampaignEmailReceivedCommand(
                    messageId,
                    workItem.DomainId,
                    workItem.SubdomainId,
                    message.Subject,
                    message.Date,
                    campaignId.Value,
                    addresses), cancellationToken);
        }

        var messagesDir = Path.Combine(contentPath, "messages");
        Directory.CreateDirectory(messagesDir);
        var filePath = Path.Combine(messagesDir, $"{messageId}.eml");
        await message.WriteToAsync(filePath, cancellationToken);
    }
}