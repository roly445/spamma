using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Fido2NetLib;
using Fido2NetLib.Objects;
using JasperFx;
using Marten;
using Marten.Services.Json;
using MediatR.Behaviors.Authorization.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nager.PublicSuffix;
using Nager.PublicSuffix.RuleProviders;
using Nager.PublicSuffix.RuleProviders.CacheProviders;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Spamma.App.Client.Infrastructure.Auth;
using Spamma.App.Client.Infrastructure.Constants;
using Spamma.App.Components;
using Spamma.App.Infrastructure;
using Spamma.App.Infrastructure.Configuration;
using Spamma.App.Infrastructure.Contracts;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.App.Infrastructure.Contracts.Settings;
using Spamma.App.Infrastructure.Endpoints;
using Spamma.App.Infrastructure.Endpoints.Maintenance;
using Spamma.App.Infrastructure.Hubs;
using Spamma.App.Infrastructure.Middleware;
using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.Infrastructure.Contracts;
using Spamma.Modules.DomainManagement;
using Spamma.Modules.DomainManagement.Infrastructure.Services;
using Spamma.Modules.EmailInbox;
using Spamma.Modules.UserManagement;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Infrastructure.JsonConverters;
using StackExchange.Redis;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry for logs, metrics, and traces
// Uses OTLP exporter for maximum flexibility with any backend
// Configure endpoint via environment: OTEL_EXPORTER_OTLP_ENDPOINT=http://your-backend:4317
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter())
    .WithMetrics(metrics =>
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter())
    .ConfigureResource(r => r.AddService("spamma"));

builder.Logging.AddOpenTelemetry(options =>
    options.AddOtlpExporter());

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add database configuration early
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var loggerFactory = LoggerFactory.Create(f => f.AddConsole());

builder.Configuration.AddDatabaseConfiguration(connectionString, loggerFactory);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(
        options => options.SerializeAllClaims = true);

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ICommander, Commander>();
builder.Services.AddScoped<IQuerier, Querier>();

// Configure data protection for consistent cookie encryption across container restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/certs/keys"));

builder.Services.Configure<Settings>(opt => builder.Configuration.GetSection("Settings").Bind(opt));
builder.Services.Configure<SetupSettings>(opt => builder.Configuration.GetSection("Setup").Bind(opt));

builder.Services.AddUserManagement()
    .AddDomainManagement().AddEmailInbox();

builder.Services.Configure<JsonOptions>(options =>
{
    // Add global byte array converter for WebAuthn credential data
    options.SerializerOptions.Converters.Add(new ByteArrayJsonConverter());

    options.AddJsonConvertersForUserManagement()
        .AddJsonConvertersForDomainManagement()
        .AddJsonConvertersForEmailInbox();
});

builder.Services.AddScoped<IAppConfigurationService, AppConfigurationService>();

// Add Marten (without Configuration entity since we're using Dapper)
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
    options.AutoCreateSchemaObjects = AutoCreate.All;

    // Note: UseSystemTextJsonForSerialization is called in ConfigureUserManagement()
    // to apply our ByteArrayJsonConverter for passkey data
    options.ConfigureUserManagement()
        .ConfigureDomainManagement()
        .ConfigureEmailInbox();

    options.Logger(new ConsoleMartenLogger());
}).ApplyAllDatabaseChangesOnStartup();

builder.Services.AddMediatorAuthorization(typeof(MustBeAuthenticatedRequirement).Assembly);
builder.Services.AddAuthorizersFromAssembly(typeof(MustBeAuthenticatedRequirement).Assembly);

builder.Services.AddCap(capOptions =>
{
    capOptions.UseRedis(builder.Configuration.GetConnectionString("Redis")!);
    capOptions.UsePostgreSql(connectionString);
    capOptions.UseDashboard();
})
    .AddSubscriberAssembly(typeof(Spamma.Modules.UserManagement.Module).Assembly, typeof(Program).Assembly, typeof(Spamma.Modules.EmailInbox.Module).Assembly);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

// FluentEmail now uses database configuration with fallbacks
builder.Services
    .AddFluentEmail(
        builder.Configuration["Settings:FromEmailAddress"] ?? "noreply@example.com",
        builder.Configuration["Settings:FromName"] ?? "Spamma")
    .AddRazorRenderer()
    .AddSmtpSender(() =>
    {
        var client = new SmtpClient();
        client.Host = builder.Configuration["Settings:EmailSmtpHost"] ?? "localhost";
        client.Port = int.Parse(builder.Configuration["Settings:EmailSmtpPort"] ?? "587");
        var username = builder.Configuration["Settings:EmailSmtpUsername"];
        if (username != null)
        {
            client.Credentials = new NetworkCredential
            {
                UserName = username,
                Password = builder.Configuration["Settings:EmailSmtpPassword"] ?? string.Empty,
            };
        }

        if (bool.TryParse(builder.Configuration["Settings:EmailSmtpUseTls"], out var useSsl))
        {
            client.EnableSsl = useSsl;
        }

        return client;
    });

// Add SignalR
builder.Services.AddSignalR();

// Register the notification service
builder.Services.AddScoped<IClientNotifierService, ClientNotifierService>();

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<IAuthTokenProvider, AuthTokenProvider>();

builder.Services.AddSingleton<IDirectoryWrapper, DirectoryWrapper>();
builder.Services.AddSingleton<IFileWrapper, FileWrapper>();

builder.Services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHttpContextAccessor();

// JWT configuration with database fallbacks
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddJwtBearer("jwt", options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (!string.IsNullOrEmpty(jwtKey))
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Spamma",
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Spamma",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            };
        }
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.SessionStore = new RedisCacheTicketStore(new RedisCacheOptions
        {
            Configuration = builder.Configuration.GetConnectionString("Redis"),
        });
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "SpammaAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events.OnValidatePrincipal = async context =>
        {
            var userIdRaw = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdRaw == null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }

            if (!Guid.TryParse(userIdRaw, out var userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }

            var userStatusCache = context.HttpContext.RequestServices.GetRequiredService<UserStatusCache>();

            var isDisabled = await userStatusCache.GetUserLookupAsync(userId);

            if (isDisabled.HasNoValue)
            {
                // Not in cache: fetch from DB, then cache it
                var userManager = context.HttpContext.RequestServices.GetRequiredService<IQuerier>();
                var tempObjectStore = context.HttpContext.RequestServices.GetRequiredService<IInternalQueryStore>();
                var query = new GetUserByIdQuery(userId);
                tempObjectStore.StoreQueryRef(query);
                var user = await userManager.Send(query);

                if (user.Status != QueryResultStatus.Succeeded || user.Data.IsSuspended)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync();
                    return;
                }

                await userStatusCache.SetUserLookupAsync(new UserStatusCache.CachedUser
                {
                    IsSuspended = user.Data.IsSuspended,
                    ModeratedDomains = user.Data.ModeratedDomains.ToList(),
                    ModeratedSubdomains = user.Data.ModeratedSubdomains.ToList(),
                    SystemRole = user.Data.SystemRole,
                    EmailAddress = user.Data.EmailAddress,
                    Name = user.Data.Name,
                    UserId = user.Data.Id,
                });
            }
            else if (isDisabled.HasValue && isDisabled.Value.IsSuspended)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "UserAdministration",
        policy => policy.Requirements.Add(new BitwiseRoleRequirement(SystemRole.UserManagement)));

    options.AddPolicy(
        "DomainAdministration",
        policy => policy.Requirements.Add(new BitwiseRoleRequirement(SystemRole.DomainManagement)));

    options.AddPolicy(
        "AssignedToAnyDomain",
        policy => policy.Requirements.Add(new AssignedToAnyDomainRequirement()));
});

// Add Redis distributed cache for sessions
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Update to use scoped services
builder.Services.AddSingleton<ISetupDetectionService, SetupDetectionService>();
builder.Services.AddSingleton<IInMemorySetupAuthService, InMemorySetupAuthService>();

builder.Services.AddScoped<UserStatusCache>();
builder.Services.AddSingleton<IInternalQueryStore, InternalQueryStore>();

// Register ACME certificate services
builder.Services.AddSingleton<AcmeChallengeServer>();
builder.Services.AddSingleton<IAcmeChallengeResponder>(sp => sp.GetRequiredService<AcmeChallengeServer>());
builder.Services.AddHostedService<CertificateRenewalBackgroundService>();

builder.Services.AddHttpClient(); // Required for CachedHttpRuleProvider
builder.Services.AddSingleton<ICacheProvider, LocalFileSystemCacheProvider>();
builder.Services.AddSingleton<IRuleProvider, CachedHttpRuleProvider>();
builder.Services.AddSingleton<IDomainParser, DomainParser>();

builder.Host.ApplyJasperFxExtensions();

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAuthorization();
app.UseAntiforgery();
app.UseSession();
app.UseAcmeChallenge();
app.UseMiddleware<SetupModeMiddleware>();

var ruleProvider = app.Services.GetService<IRuleProvider>();
if (ruleProvider != null)
{
    await ruleProvider.BuildAsync();
}

// Initialize domain parser service with the built rule provider
var domainParser = app.Services.GetService<IDomainParser>();
var domainParserService = app.Services.GetService<IDomainParserService>();
if (domainParser != null && domainParserService != null)
{
    domainParserService.SetDomainParser(domainParser);
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Spamma.App.Client._Imports).Assembly);

app.AddUserManagementApi()
    .AddDomainManagementApi()
    .AddEmailInboxApi();

// Map API endpoints organized by feature
app.MapGeneralApiEndpoints();
app.MapAuthenticationEndpoints();
app.MapMaintenanceEndpoints();

app.MapHub<NotifierHub>($"/{Lookups.NotificationHubName}");

return await app.RunJasperFxCommands(args);