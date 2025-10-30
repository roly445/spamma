using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nager.PublicSuffix;
using Nager.PublicSuffix.RuleProviders;

namespace Spamma.Modules.DomainManagement.Infrastructure.Services;

public class DomainParserInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DomainParserInitializationService(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var ruleProvider = new SimpleHttpRuleProvider();
        await ruleProvider.BuildAsync();
        var domainParser = new DomainParser(ruleProvider);

        var service = this._serviceProvider.GetRequiredService<IDomainParserService>();
        if (service is DomainParserService domainParserService)
        {
            domainParserService.SetDomainParser(domainParser);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
