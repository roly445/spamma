// See https://aka.ms/new-console-template for more information

using FluentEmail.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services
            .AddFluentEmail("fromemail@test.test")
            .AddSmtpSender("localhost", 9025);
    })
    .ConfigureLogging((_, logging) =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(options => options.IncludeScopes = true);
    })
    .Build();
var email = host.Services.GetRequiredService<IFluentEmail>();
var response = await email.To("an@addresscom").Body("something here").Subject("subject").SendAsync();
Console.WriteLine(response.Successful);