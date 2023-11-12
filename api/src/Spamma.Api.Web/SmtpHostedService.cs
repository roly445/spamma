namespace Spamma.Api.Web
{
    public class SmtpHostedService : BackgroundService
    {
        private readonly SmtpServer.SmtpServer _smtpServer;

        public SmtpHostedService(SmtpServer.SmtpServer smtpServer)
        {
            this._smtpServer = smtpServer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this._smtpServer.StartAsync(stoppingToken);
        }
    }
}