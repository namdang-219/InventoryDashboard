using IID.Application.Common.Interfaces;
using IID.Application.Common.Messaging;
using IID.Application.Features.Dashboard.Services;
using IID.Infrastructure.Configuration;
using IID.Infrastructure.Messaging;
using IID.Infrastructure.Persistence;
using IID.Infrastructure.Persistence.Repositories;
using IID.ReportGenerator.Configurations;
using IID.ReportGenerator.HostedServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace IID.ReportGenerator.Extensions;

public static class ServiceCollectionExtensions
{
    public static HostApplicationBuilder ConfigureSerilogWithOpenObserve(this HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        builder.Services.AddSerilog((services, lc) =>
        {
            lc.ReadFrom.Configuration(builder.Configuration)
              .Enrich.FromLogContext();

            var config = builder.Configuration
                .GetSection(OpenObserveConfiguration.Name)
                .Get<OpenObserveConfiguration>() ?? new OpenObserveConfiguration();

            if (!config.Enabled || string.IsNullOrWhiteSpace(config.Endpoint))
                return;

            var endpoint = config.Endpoint.TrimEnd('/');
            var env = string.IsNullOrWhiteSpace(config.Environment)
                ? builder.Environment.EnvironmentName
                : config.Environment;

            lc.WriteTo.OpenTelemetry(opts =>
            {
                opts.Endpoint = $"{endpoint}/v1/logs";
                opts.Protocol = OtlpProtocol.HttpProtobuf;
                if (!string.IsNullOrWhiteSpace(config.AuthToken))
                {
                    opts.Headers = new Dictionary<string, string>
                    {
                        ["Authorization"] = $"Basic {config.AuthToken}",
                        ["stream-name"] = config.StreamName
                    };
                }
                opts.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = config.ServiceName,
                    ["service.version"] = config.ServiceVersion,
                    ["deployment.environment"] = env
                };
            });
        });

        return builder;
    }

    public static IServiceCollection ConfigureReportGeneratorServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .Configure<KafkaConfiguration>(configuration.GetSection(KafkaConfiguration.Name))
            .Configure<ReportGeneratorOptions>(configuration.GetSection(ReportGeneratorOptions.Name))
            .Configure<OpenObserveConfiguration>(configuration.GetSection(OpenObserveConfiguration.Name));

        var connStr = configuration.GetConnectionString("IID")
            ?? throw new InvalidOperationException("ConnectionStrings:IID missing for IID.ReportGenerator.");

        services.AddDbContext<IidDbContext>(opts =>
        {
            opts.UseSqlServer(connStr);
            opts.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // Core services & repositories required for snapshot calculation and persistence
        services.AddScoped<IClock, IID.Infrastructure.System.SystemClock>();
        services.AddScoped<IDealershipRepository, DealershipRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IDashboardSnapshotRepository, DashboardSnapshotRepository>();
        services.AddScoped<IDashboardSnapshotCalculator, DashboardSnapshotCalculator>();
        services.AddScoped<IDashboardSnapshotRefresher, DashboardSnapshotRefresher>();

        // Kafka messaging
        services.AddSingleton<IKafkaEventProducer, ConfluentKafkaEventProducer>();
        services.AddSingleton<KafkaTopicProvisioner>();

        // Worker hosted services
        services.AddSingleton<DashboardBootstrapReadiness>();
        services.AddHostedService<DashboardBootstrapHostedService>();
        services.AddHostedService<VehicleMutationConsumerHostedService>();
        services.AddHostedService<AgingRolloverHostedService>();

        services.AddReportGeneratorObservability(configuration);

        return services;
    }

    private static IServiceCollection AddReportGeneratorObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = configuration
            .GetSection(OpenObserveConfiguration.Name)
            .Get<OpenObserveConfiguration>() ?? new OpenObserveConfiguration();

        if (!config.Enabled || string.IsNullOrWhiteSpace(config.Endpoint))
            return services;

        var endpoint = config.Endpoint.TrimEnd('/');
        var environment = string.IsNullOrWhiteSpace(config.Environment)
            ? configuration["DOTNET_ENVIRONMENT"] ?? "Production"
            : config.Environment;
        var authHeader = !string.IsNullOrWhiteSpace(config.AuthToken)
            ? $"Authorization=Basic {config.AuthToken},stream-name={config.StreamName}"
            : null;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: config.ServiceName, serviceVersion: config.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(o =>
                    {
                        o.SetDbStatementForText = true;
                        o.SetDbStatementForStoredProcedure = true;
                    })
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri($"{endpoint}/v1/traces");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                        if (!string.IsNullOrWhiteSpace(authHeader))
                            o.Headers = authHeader;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddHttpClientInstrumentation()
                    .AddProcessInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri($"{endpoint}/v1/metrics");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                        if (!string.IsNullOrWhiteSpace(authHeader))
                            o.Headers = authHeader;
                    });
            });

        return services;
    }
}

