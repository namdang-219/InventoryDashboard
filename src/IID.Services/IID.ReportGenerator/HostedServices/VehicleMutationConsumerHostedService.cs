using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using IID.Application.Common.Interfaces;
using IID.Application.Common.Messaging;
using IID.Application.Features.Dashboard.Services;
using IID.Domain.Dashboard;
using IID.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace IID.ReportGenerator.HostedServices;

/// <summary>
/// Consumes <see cref="VehicleMutatedIntegrationEvent"/> messages from Apache Kafka,
/// coalesces bursts within a configurable debounce window, regenerates affected
/// static dashboard snapshots with bounded retries, routes persistently failing batches
/// to the dead-letter topic, and commits offsets only after persistence or DLQ handoff succeeds.
/// </summary>
public sealed class VehicleMutationConsumerHostedService(
    IServiceScopeFactory scopeFactory,
    IKafkaEventProducer producer,
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
                var startOffsets = new Dictionary<TopicPartition, TopicPartitionOffset>();

                try
                {
                    var first = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (first?.Message is null)
                        continue;

                    var batch = new List<VehicleMutatedIntegrationEvent>();
                    var offsets = new Dictionary<TopicPartition, TopicPartitionOffset>();

                    CollectMessage(first, batch, offsets, startOffsets);

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

                            CollectMessage(next, batch, offsets, startOffsets);
                        }
                    }

                    if (batch.Count > 0)
                    {
                        await ProcessBatchWithRetryAsync(batch, stoppingToken);
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
                catch (Exception ex) when (ex is not OperationCanceledException && !stoppingToken.IsCancellationRequested)
                {
                    SeekToBatchStart(consumer, startOffsets);
                    logger.LogError(ex, "Error processing or dead-lettering dashboard snapshot batch; rewound offsets and will retry in 2s.");
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

    public async Task ProcessBatchWithRetryAsync(
        IReadOnlyList<VehicleMutatedIntegrationEvent> batch,
        CancellationToken stoppingToken)
    {
        var maxRetries = Math.Max(0, _options.MutationMaxRetryCount);
        var baseDelayMs = Math.Max(0, _options.MutationRetryDelayMs);

        for (var retry = 0; ; retry++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var refresher = scope.ServiceProvider.GetRequiredService<IDashboardSnapshotRefresher>();
                await refresher.RefreshForMutationsAsync(batch, stoppingToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException && !stoppingToken.IsCancellationRequested)
            {
                if (retry < maxRetries)
                {
                    var backoffMs = Math.Min(30_000, baseDelayMs * (1L << Math.Min(retry, 10)));
                    var delay = TimeSpan.FromMilliseconds(backoffMs);

                    logger.LogWarning(
                        ex,
                        "RefreshForMutationsAsync failed for batch of {Count} mutation(s); retrying ({RetryAttempt}/{MaxRetries}) in {DelayMs}ms.",
                        batch.Count,
                        retry + 1,
                        maxRetries,
                        delay.TotalMilliseconds);

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, stoppingToken);
                    }

                    continue;
                }

                await PublishToDeadLetterQueueAsync(batch, maxRetries, ex, stoppingToken);
                return;
            }
        }
    }

    private async Task PublishToDeadLetterQueueAsync(
        IReadOnlyList<VehicleMutatedIntegrationEvent> batch,
        int retryCount,
        Exception lastException,
        CancellationToken stoppingToken)
    {
        var firstDealershipId = batch
            .Select(m => m.DealershipId)
            .FirstOrDefault(id => id.HasValue && id.Value != Guid.Empty);

        var dlqKey = firstDealershipId?.ToString("D") ?? DashboardSnapshot.GlobalScopeKey;
        var dlqEvent = new VehicleMutationDeadLetterIntegrationEvent(
            DeadLetterId: Guid.NewGuid(),
            SourceTopic: _options.VehicleMutationsTopic,
            Mutations: batch,
            RetryCount: retryCount,
            ErrorType: lastException.GetType().FullName ?? lastException.GetType().Name,
            ErrorMessage: lastException.Message,
            FailedAtUtc: DateTimeOffset.UtcNow);

        await producer.PublishAsync(KafkaTopics.VehicleMutationsDlq, dlqKey, dlqEvent, stoppingToken);

        logger.LogWarning(
            lastException,
            "RefreshForMutationsAsync still failed after {MaxRetries} retries for batch of {Count} mutation(s); routed batch (deadLetterId={DeadLetterId}) to dead-letter topic {DlqTopic}.",
            retryCount,
            batch.Count,
            dlqEvent.DeadLetterId,
            _options.VehicleMutationsDlqTopic);
    }

    private void SeekToBatchStart(
        IConsumer<string, string> consumer,
        Dictionary<TopicPartition, TopicPartitionOffset> startOffsets)
    {
        foreach (var startOffset in startOffsets.Values)
        {
            try
            {
                consumer.Seek(startOffset);
            }
            catch (Exception seekEx)
            {
                logger.LogWarning(
                    seekEx,
                    "Failed to seek Kafka consumer back to {TopicPartitionOffset}.",
                    startOffset);
            }
        }
    }

    private void CollectMessage(
        ConsumeResult<string, string> result,
        List<VehicleMutatedIntegrationEvent> batch,
        Dictionary<TopicPartition, TopicPartitionOffset> offsets,
        Dictionary<TopicPartition, TopicPartitionOffset> startOffsets)
    {
        startOffsets.TryAdd(result.TopicPartition, result.TopicPartitionOffset);
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

