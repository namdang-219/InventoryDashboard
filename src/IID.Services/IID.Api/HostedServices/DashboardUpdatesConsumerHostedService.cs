using System.Text.Json;
using Confluent.Kafka;
using IID.Application.Common.Interfaces;
using IID.Application.Common.Messaging;
using IID.Application.Features.Dashboard.Services;
using IID.Infrastructure.Configuration;
using IID.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace IID.Api.HostedServices;

/// <summary>
/// Consumes <see cref="DashboardSnapshotUpdatedIntegrationEvent"/> messages from Apache Kafka
/// and broadcasts dealership-scoped (or global) SignalR updates to connected clients.
/// </summary>
public sealed class DashboardUpdatesConsumerHostedService(
    IServiceScopeFactory scopeFactory,
    KafkaTopicProvisioner topicProvisioner,
    IOptions<KafkaConfiguration> options,
    ILogger<DashboardUpdatesConsumerHostedService> logger) : BackgroundService
{
    private readonly KafkaConfiguration _options = options.Value;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            logger.LogInformation("Kafka consumer in IID.Api is disabled (Kafka:Enabled=false).");
            return Task.CompletedTask;
        }

        return Task.Run(() => RunConsumerLoopAsync(stoppingToken), stoppingToken);
    }

    private async Task RunConsumerLoopAsync(CancellationToken stoppingToken)
    {
        try
        {
            await topicProvisioner.EnsureTopicsExistAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Kafka topic check failed on IID.Api startup; consumer will retry automatically.");
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = $"{_options.ApiGroupId}-{Environment.MachineName}",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, err) =>
            {
                if (err.IsFatal)
                    logger.LogError("Fatal Kafka consumer error in IID.Api ({Code}): {Reason}", err.Code, err.Reason);
                else
                    logger.LogDebug("Kafka consumer notice in IID.Api ({Code}): {Reason}", err.Code, err.Reason);
            })
            .Build();

        consumer.Subscribe(_options.DashboardUpdatesTopic);
        logger.LogInformation(
            "IID.Api subscribed to Kafka topic {Topic} (group={GroupId}).",
            _options.DashboardUpdatesTopic,
            config.GroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (result?.Message?.Value is null)
                        continue;

                    var evt = JsonSerializer.Deserialize<DashboardSnapshotUpdatedIntegrationEvent>(
                        result.Message.Value,
                        DashboardSnapshotSerializer.JsonOptions);

                    if (evt is null)
                        continue;

                    await using var scope = scopeFactory.CreateAsyncScope();
                    var hubNotifier = scope.ServiceProvider.GetRequiredService<IVehicleHubNotifier>();

                    await hubNotifier.DashboardSummaryUpdatedAsync(evt.Summary, stoppingToken, evt.DealershipId);
                    await hubNotifier.DashboardAlertsUpdatedAsync(evt.Alerts, stoppingToken, evt.DealershipId);
                    await hubNotifier.InventoryChangedAsync(stoppingToken, evt.DealershipId);

                    logger.LogDebug(
                        "Broadcast SignalR dashboard update for scope={ScopeKey} version={Version}.",
                        evt.ScopeKey,
                        evt.Version);
                }
                catch (ConsumeException ex)
                {
                    logger.LogWarning(ex, "Kafka consume error on {Topic}; backing off 2s.", _options.DashboardUpdatesTopic);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Unexpected error forwarding Kafka dashboard update to SignalR.");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            try
            {
                consumer.Close();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Kafka consumer close threw during IID.Api shutdown.");
            }
        }
    }
}

