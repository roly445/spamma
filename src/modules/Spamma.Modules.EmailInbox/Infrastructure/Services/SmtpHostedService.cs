using Microsoft.Extensions.Hosting;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public class SmtpHostedService(SmtpServer.SmtpServer smtpServer)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await smtpServer.StartAsync(stoppingToken);
    }
}