using System.Text;
using Azure.Messaging.ServiceBus;
using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Campaigns.ChangeCampaignStatus;
using HackatonFiap.Donations.Application.Campaigns.CreateCampaign;
using HackatonFiap.Donations.Application.Campaigns.ExpireDueCampaigns;
using HackatonFiap.Donations.Application.Campaigns.GetCampaignById;
using HackatonFiap.Donations.Application.Campaigns.ListCampaigns;
using HackatonFiap.Donations.Application.Campaigns.UpdateCampaign;
using HackatonFiap.Donations.Application.Donations.CreateDonation;
using HackatonFiap.Donations.Application.Donations.GetDonationById;
using HackatonFiap.Donations.Application.Donations.ListMyDonations;
using HackatonFiap.Donations.Application.Donations.ProcessPaymentApproved;
using HackatonFiap.Donations.Application.Donations.ProcessPaymentDeclined;
using HackatonFiap.Donations.Application.Observability;
using HackatonFiap.Donations.Application.Transparency;
using HackatonFiap.Donations.Infrastructure.BackgroundServices;
using HackatonFiap.Donations.Infrastructure.Messaging;
using HackatonFiap.Donations.Infrastructure.Persistence;
using HackatonFiap.Donations.Infrastructure.Persistence.Repositories;
using HackatonFiap.Donations.Infrastructure.ReadStore;
using HackatonFiap.Donations.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;

Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;

    builder.Host.UseSerilog((ctx, sp, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", DonationMetrics.ServiceName)
        .WriteTo.Console());

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Database (SQL Server)
    var connectionString = configuration.GetValue<string>("ConnectionStrings:Default");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:Default must be configured.");
    }
    builder.Services.AddDbContext<DonationsDbContext>(opt =>
        opt.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(6, TimeSpan.FromSeconds(30), null)));

    // Repositories, UoW, clock
    builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
    builder.Services.AddScoped<IDonationRepository, DonationRepository>();
    builder.Services.AddScoped<IProcessedEventStore, ProcessedEventStore>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddSingleton<IClock, SystemClock>();

    // CQRS handlers
    builder.Services.AddScoped<CreateCampaignCommandHandler>();
    builder.Services.AddScoped<UpdateCampaignCommandHandler>();
    builder.Services.AddScoped<ChangeCampaignStatusCommandHandler>();
    builder.Services.AddScoped<GetCampaignByIdQueryHandler>();
    builder.Services.AddScoped<ListCampaignsQueryHandler>();
    builder.Services.AddScoped<ExpireDueCampaignsCommandHandler>();
    builder.Services.AddScoped<CreateDonationCommandHandler>();
    builder.Services.AddScoped<GetDonationByIdQueryHandler>();
    builder.Services.AddScoped<ListMyDonationsQueryHandler>();
    builder.Services.AddScoped<ProcessPaymentApprovedCommandHandler>();
    builder.Services.AddScoped<ProcessPaymentDeclinedCommandHandler>();
    builder.Services.AddScoped<ListActiveCampaignsQueryHandler>();

    // Read store: Cosmos quando configurado; senão fallback in-memory (Development)
    var cosmosConnection = configuration.GetValue<string>("Cosmos:ConnectionString");
    if (!string.IsNullOrWhiteSpace(cosmosConnection))
    {
        var cosmosOptions = new CosmosOptions
        {
            ConnectionString = cosmosConnection,
            Database = configuration.GetValue<string>("Cosmos:Database") ?? "HackatonFiapDonations",
            Container = configuration.GetValue<string>("Cosmos:Container") ?? "campaigns"
        };
        builder.Services.AddSingleton(cosmosOptions);
        builder.Services.AddSingleton(_ => new CosmosClient(cosmosConnection));
        builder.Services.AddSingleton<ICampaignReadStore, CosmosCampaignReadStore>();
    }
    else if (builder.Environment.IsDevelopment())
    {
        Log.Warning("Cosmos:ConnectionString não configurado — usando InMemoryCampaignReadStore (Development).");
        builder.Services.AddSingleton<ICampaignReadStore, InMemoryCampaignReadStore>();
    }
    else
    {
        throw new InvalidOperationException("Cosmos:ConnectionString must be configured outside Development.");
    }

    // Service Bus: publisher (tópico de requisição) + consumer (tópico de resultado)
    var sbConnection = configuration.GetValue<string>("ServiceBus:ConnectionString");
    if (!string.IsNullOrWhiteSpace(sbConnection))
    {
        var requestTopic = configuration.GetValue<string>("ServiceBus:RequestTopic") ?? "donation-requested";
        var resultTopic = configuration.GetValue<string>("ServiceBus:ResultTopic") ?? "payment-result";
        var resultSubscription = configuration.GetValue<string>("ServiceBus:ResultSubscription") ?? "donations";

        builder.Services.AddSingleton(_ => new ServiceBusClient(sbConnection));
        builder.Services.AddSingleton<IEventPublisher>(sp => new ServiceBusEventPublisher(
            sp.GetRequiredService<ServiceBusClient>(), requestTopic,
            sp.GetRequiredService<ILogger<ServiceBusEventPublisher>>()));
        builder.Services.AddHostedService(sp => new PaymentResultConsumer(
            sp.GetRequiredService<ServiceBusClient>(), resultTopic, resultSubscription,
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<PaymentResultConsumer>>()));
    }
    else if (builder.Environment.IsDevelopment())
    {
        Log.Warning("ServiceBus:ConnectionString não configurado — NoOpEventPublisher e consumer desabilitado (Development).");
        builder.Services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
    }
    else
    {
        throw new InvalidOperationException("ServiceBus:ConnectionString must be configured outside Development.");
    }

    // Worker de expiração (sempre ativo)
    var expirationInterval = TimeSpan.FromSeconds(configuration.GetValue<int?>("Campaigns:ExpirationScanIntervalSeconds") ?? 60);
    builder.Services.AddHostedService(sp => new CampaignExpirationWorker(
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<ILogger<CampaignExpirationWorker>>(),
        expirationInterval));

    // Auth (JWT) — mesmas issuer/audience do ecossistema
    var jwtKey = configuration.GetValue<string>("Jwt:Key");
    var jwtIssuer = configuration.GetValue<string>("Jwt:Issuer") ?? "conexaosolidaria.local";
    var jwtAudience = configuration.GetValue<string>("Jwt:Audience") ?? "conexaosolidaria.clients";
    if (string.IsNullOrEmpty(jwtKey) && !builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Jwt:Key must be configured outside Development.");
    }
    jwtKey ??= Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = true
            };
        });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ManagersOnly", p => p.RequireRole("GestorONG", "Owner"));
        options.AddPolicy("DonorOnly", p => p.RequireRole("Doador"));
    });

    // Observability
    builder.Services.AddOpenTelemetry().WithMetrics(m => m
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(DonationMetrics.ServiceName))
        .AddMeter(DonationMetrics.ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<DonationsDbContext>(name: "sqlserver", tags: new[] { "ready" });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.GetRequiredService<DonationsDbContext>().Database.Migrate();
    }

    app.UseSerilogRequestLogging();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });
    app.MapPrometheusScrapingEndpoint("/metrics");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
