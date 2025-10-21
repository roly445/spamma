using System.Security.Claims;
using System.Text;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using JasperFx;
using Marten;
using MediatR.Behaviors.Authorization.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Spamma.App.Client.Infrastructure.Auth;
using Spamma.App.Components;
using Spamma.App.Infrastructure;
using Spamma.App.Infrastructure.Configuration;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.App.Infrastructure.Contracts.Settings;
using Spamma.App.Infrastructure.Middleware;
using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.DomainManagement;
using Spamma.Modules.EmailInbox;
using Spamma.Modules.UserManagement;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Client.Contracts;
using StackExchange.Redis;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.Configure<Settings>(opt => builder.Configuration.GetSection("Settings").Bind(opt));
builder.Services.Configure<SetupSettings>(opt => builder.Configuration.GetSection("Setup").Bind(opt));

builder.Services.AddUserManagement()
    .AddDomainManagement().AddEmailInbox();

builder.Services.Configure<JsonOptions>(options =>
{
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
    options.UseSystemTextJsonForSerialization();

    options.ConfigureUserManagement()
        .ConfigureDomainManagement()
        .ConfigureEmailInbox();
});

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
    .AddSmtpSender(
        builder.Configuration["Settings:EmailSmtpHost"] ?? "localhost",
        int.Parse(builder.Configuration["Settings:EmailSmtpPort"] ?? "587"));

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
                var tempObjectStore = context.HttpContext.RequestServices.GetRequiredService<ITempObjectStore>();
                var query = new GetUserByIdQuery(userId);
                tempObjectStore.AddReferenceForObject(query);
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
                    Domains = user.Data.ModeratedDomains.ToList(),
                    Subdomains = user.Data.ModeratedSubdomains.ToList(),
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
builder.Services.AddSingleton<ITempObjectStore, TempObjectStore>();

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
    app.UseHsts();
}

app.UseAuthorization();
app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseSession();
app.UseMiddleware<SetupModeMiddleware>();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Spamma.App.Client._Imports).Assembly);

app.AddUserManagementApi()
    .AddDomainManagementApi()
    .AddEmailInboxApi();

app.MapGet("dynamicsettings.json",   (IOptions<Settings> settings) => Results.Json(new
{
    Settings = new
    {
        settings.Value.MailServerHostname,
        settings.Value.MxPriority,
    },
}));

return await app.RunJasperFxCommands(args);