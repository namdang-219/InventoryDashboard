using System.Text.Json;
using Confluent.Kafka;
using IID.Application.Common.Messaging;
using IID.Application.Features.Dashboard.Services;
using IID.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IID.Infrastructure.Messaging;

/// <summary>
/// Singleton Confluent.Kafka producer with lazy broker connection initialization.
/// Safely degrades to a no-op when <c>Kafka:Enabled</c> is false.
/// </summary>
public sealed class ConfluentKafkaEventProducer : IKafkaEventProducer, IDisposable
{
    private readonly KafkaConfiguration _options;
    private readonly ILogger<ConfluentKafkaEventProducer> _logger;
    private readonly Lazy<IProducer<string, string>> _producer;

    public ConfluentKafkaEventProducer(
        IOptions<KafkaConfiguration> options,
        ILogger<ConfluentKafkaEventProducer> logger)
    {
        _options = options.Value;
        _logger = logger;
        _producer = new Lazy<IProducer<string, string>>(BuildProducer, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task PublishAsync<TMessage>(
        string topic,
        string key,
        TMessage message,
        CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            _logger.LogDebug("Kafka producer disabled; skipping publish to topic {Topic} (key={Key}).", topic, key);
            return;
        }

        var targetTopic = ResolveConfiguredTopic(topic);
        var payload = JsonSerializer.Serialize(message, DashboardSnapshotSerializer.JsonOptions);

        var delivery = await _producer.Value.ProduceAsync(
            targetTopic,
            new Message<string, string>
            {
                Key = key,
                Value = payload
            },
            ct);

        _logger.LogDebug(
            "Published Kafka event to {TopicPartitionOffset} (key={Key}).",
            delivery.TopicPartitionOffset,
            key);
    }

    private string ResolveConfiguredTopic(string topic)
        => topic switch
        {
            KafkaTopics.VehicleMutations => _options.VehicleMutationsTopic,
            KafkaTopics.DashboardUpdates => _options.DashboardUpdatesTopic,
            _ => topic
        };

    private IProducer<string, string> BuildProducer()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = _options.MessageTimeoutMs
        };

        return new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, err) =>
            {
                if (err.IsFatal)
                    _logger.LogError("Fatal Kafka producer error ({Code}): {Reason}", err.Code, err.Reason);
                else
                    _logger.LogWarning("Kafka producer warning ({Code}): {Reason}", err.Code, err.Reason);
            })
            .Build();
    }

    public void Dispose()
    {
        if (!_producer.IsValueCreated)
            return;

        try
        {
            _producer.Value.Flush(TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Kafka producer flush threw during shutdown.");
        }
        finally
        {
            _producer.Value.Dispose();
        }
    }
}

