using IID.Api.Configurations;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace IID.Api.Extensions;

/// <summary>
/// Serilog and OpenObserve logging pipeline configuration.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures Serilog as the sole logging provider and wires OpenObserve OTLP log ingestion if enabled.
    /// </summary>
    public static WebApplicationBuilder ConfigureSerilogWithOpenObserve(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        builder.Host.UseSerilog((ctx, lc) =>
        {
            lc.ReadFrom.Configuration(ctx.Configuration)
              .Enrich.FromLogContext();

            var config = ctx.Configuration
                .GetSection(OpenObserveConfiguration.Name)
                .Get<OpenObserveConfiguration>() ?? new OpenObserveConfiguration();

            if (!config.Enabled)
                return;

            var endpoint = config.Endpoint.TrimEnd('/');
            var env = string.IsNullOrWhiteSpace(config.Environment)
                ? ctx.HostingEnvironment.EnvironmentName
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
}
