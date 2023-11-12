using Fluxera.Repository;
using Fluxera.Repository.EntityFrameworkCore;
using SmtpServer;
using SmtpServer.Storage;
using Spamma.Api.Data;
using Spamma.Api.Data.Sqlite;
using Spamma.Api.Domain.MailMessage;
using Spamma.Api.Domain.MailMessage.Aggregate;
using Spamma.Api.Domain.MailMessage.CommandHandlers;
using Spamma.Api.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SpammaSqliteContext>();
builder.Services.AddScoped<SpammaContext, SpammaSqliteContext>();
builder.Services.AddTransient<IMessageStore, SpammaMessageStore>();
builder.Services.AddSingleton(
    provider =>
    {
        var options = new SmtpServerOptionsBuilder()
            .ServerName("SMTP Server")
            .Port(9025)
            .Build();

        return new SmtpServer.SmtpServer(options, provider.GetRequiredService<IServiceProvider>());
    });
builder.Services.AddTransient<IMailMessageRepository, MailMessageRepository>();
builder.Services.AddHostedService<SmtpHostedService>();
builder.Services.AddRepository(repositoryBuilder =>
{
    repositoryBuilder.AddEntityFrameworkRepository<FluxeraContext>(repositoryOptionsBuilder =>
    {
        repositoryOptionsBuilder.UseFor<MailMessage>();
    });
});

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<StoreMessageCommandHandler>());

var app = builder.Build();

app.Run();