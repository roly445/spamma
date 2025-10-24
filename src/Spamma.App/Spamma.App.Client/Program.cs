using BluQube.Commands;
using BluQube.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Spamma.App.Client.Infrastructure.Auth;
using Spamma.App.Client.Infrastructure.Constants;
using Spamma.App.Client.Infrastructure.Contracts;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.App.Client.Infrastructure.Services;
using Spamma.Modules.DomainManagement.Client;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.UserManagement.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
await LoadAppSettingsAsync(builder);

// Now you can configure IOptions as usual
builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));

builder.Services.AddScoped<ICommander, Commander>();
builder.Services.AddScoped<IQuerier, Querier>();
builder.Services.AddScoped<IDomainValidationService, DomainValidationService>();

builder.Services.AddHttpClient(
    "bluqube",
    client => { client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress); });
builder.Services.AddTransient<CommandResultConverter>();

builder.Services.AddUserManagement()
    .AddDomainManagement()
    .AddEmailInbox();
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(
        Lookups.UserAdministration,
        policy => policy.Requirements.Add(new BitwiseRoleRequirement(SystemRole.UserManagement)));

    options.AddPolicy(
        Lookups.DomainAdministration,
        policy => policy.Requirements.Add(new BitwiseRoleRequirement(SystemRole.DomainManagement)));

    options.AddPolicy(
        Lookups.AssignedToAnyDomain,
        policy => policy.Requirements.Add(new AssignedToAnyDomainRequirement()));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

builder.Services.AddScoped<IAuthorizationHandler, BitwiseRoleHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AssignedToAnyDomainHandler>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<ISignalRService, SignalRService>();

var host = builder.Build();

await host.RunAsync();
return;

static async Task LoadAppSettingsAsync(WebAssemblyHostBuilder builder)
{
    var client = new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    using var response = await client.GetAsync("dynamicsettings.json");
    await using var stream = await response.Content.ReadAsStreamAsync();
    builder.Configuration.AddJsonStream(stream);
}