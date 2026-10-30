using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using IID.Application.Common.Interfaces;
using IID.Application.Common.Messaging;
using IID.Application.Features.Dashboard.Services;
using IID.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace IID.ReportGenerator.HostedServices;

/// <summary>
/// Consumes <see cref="VehicleMutatedIntegrationEvent"/> messages from Apache Kafka,
/// coalesces bursts within a configurable debounce window, regenerates affected
/// static dashboard snapshots, and commits offsets only after persistence succeeds.
/// </summary>
public sealed class VehicleMutationConsumerHostedService(
    IServiceScopeFactory scopeFactory,
    DashboardBootstrapReadiness readiness,
    IOptions<KafkaConfiguration> options,
    ILogger<VehicleMutationConsumerHostedService> logger) : BackgroundService
{
    private readonly KafkaConfiguration _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await readiness.WaitUntilReadyAsync(stoppingToken);

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            logger.LogWarning("Kafka is disabled; VehicleMutationConsumerHostedService will remain idle.");
            return;
        }

        await Task.Run(() => RunConsumerLoopAsync(stoppingToken), stoppingToken);
    }

    private async Task RunConsumerLoopAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ReportGeneratorGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, err) =>
            {
                if (err.IsFatal)
                    logger.LogError("Fatal Kafka consumer error in IID.ReportGenerator ({Code}): {Reason}", err.Code, err.Reason);
                else
                    logger.LogWarning("Kafka consumer warning in IID.ReportGenerator ({Code}): {Reason}", err.Code, err.Reason);
            })
            .Build();

        consumer.Subscribe(_options.VehicleMutationsTopic);
        logger.LogInformation(
            "IID.ReportGenerator subscribed to Kafka topic {Topic} (group={GroupId}, debounce={DebounceMs}ms).",
            _options.VehicleMutationsTopic,
            _options.ReportGeneratorGroupId,
            _options.DebounceWindowMs);

        var debounceWindow = TimeSpan.FromMilliseconds(Math.Clamp(_options.DebounceWindowMs, 0, 5000));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var first = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (first?.Message is null)
                        continue;

                    var batch = new List<VehicleMutatedIntegrationEvent>();
                    var offsets = new Dictionary<TopicPartition, TopicPartitionOffset>();

                    CollectMessage(first, batch, offsets);

                    // Coalesce burst mutations arriving within DebounceWindowMs
                    if (debounceWindow > TimeSpan.Zero)
                    {
                        var sw = Stopwatch.StartNew();
                        while (sw.Elapsed < debounceWindow && !stoppingToken.IsCancellationRequested)
                        {
                            var remaining = debounceWindow - sw.Elapsed;
                            if (remaining <= TimeSpan.Zero)
                                break;

                            var next = consumer.Consume(remaining);
                            if (next?.Message is null)
                                break;

                            CollectMessage(next, batch, offsets);
                        }
                    }

                    if (batch.Count > 0)
                    {
                        await using var scope = scopeFactory.CreateAsyncScope();
                        var refresher = scope.ServiceProvider.GetRequiredService<IDashboardSnapshotRefresher>();
                        await refresher.RefreshForMutationsAsync(batch, stoppingToken);
                    }

                    if (offsets.Count > 0)
                    {
                        consumer.Commit(offsets.Values);
                    }
                }
                catch (ConsumeException ex)
                {
                    logger.LogWarning(ex, "Kafka consume error on {Topic}; retrying in 2s.", _options.VehicleMutationsTopic);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Error regenerating static dashboard snapshot(s); offsets not committed and will retry.");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
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
                logger.LogDebug(ex, "Kafka consumer close threw during IID.ReportGenerator shutdown.");
            }
        }
    }

    private void CollectMessage(
        ConsumeResult<string, string> result,
        List<VehicleMutatedIntegrationEvent> batch,
        Dictionary<TopicPartition, TopicPartitionOffset> offsets)
    {
        offsets[result.TopicPartition] = new TopicPartitionOffset(result.TopicPartition, result.Offset + 1);

        if (string.IsNullOrWhiteSpace(result.Message.Value))
            return;

        try
        {
            var evt = JsonSerializer.Deserialize<VehicleMutatedIntegrationEvent>(
                result.Message.Value,
                DashboardSnapshotSerializer.JsonOptions);

            if (evt is not null)
                batch.Add(evt);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Skipping malformed VehicleMutatedIntegrationEvent at {TopicPartitionOffset}.",
                result.TopicPartitionOffset);
        }
    }
}

