using Confluent.Kafka;
using Confluent.Kafka.Admin;
using IID.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IID.Infrastructure.Messaging;

/// <summary>
/// Ensures required Kafka topics exist before consumers subscribe.
/// </summary>
public sealed class KafkaTopicProvisioner(
    IOptions<KafkaConfiguration> options,
    ILogger<KafkaTopicProvisioner> logger)
{
    private readonly KafkaConfiguration _options = options.Value;

    public async Task EnsureTopicsExistAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            logger.LogInformation("Kafka is disabled; skipping topic provisioning.");
            return;
        }

        var topics = new[]
        {
            _options.VehicleMutationsTopic,
            _options.DashboardUpdatesTopic
        }.Distinct(StringComparer.Ordinal).ToArray();

        const int maxAttempts = 15;
        for (var attempt = 1; attempt <= maxAttempts && !ct.IsCancellationRequested; attempt++)
        {
            try
            {
                using var admin = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = _options.BootstrapServers
                }).Build();

                var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
                var existing = metadata.Topics
                    .Where(t => !t.Error.IsError)
                    .Select(t => t.Topic)
                    .ToHashSet(StringComparer.Ordinal);

                var specs = topics
                    .Where(t => !existing.Contains(t))
                    .Select(t => new TopicSpecification
                    {
                        Name = t,
                        NumPartitions = Math.Max(1, _options.TopicPartitions),
                        ReplicationFactor = Math.Max((short)1, _options.TopicReplicationFactor)
                    })
                    .ToList();

                if (specs.Count == 0)
                {
                    logger.LogInformation("Kafka topics [{Topics}] already exist.", string.Join(", ", topics));
                    return;
                }

                await admin.CreateTopicsAsync(specs);
                logger.LogInformation("Created Kafka topics [{Topics}].", string.Join(", ", specs.Select(s => s.Name)));
                return;
            }
            catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code is ErrorCode.TopicAlreadyExists or ErrorCode.NoError))
            {
                logger.LogInformation("Kafka topics [{Topics}] already created concurrently.", string.Join(", ", topics));
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(10, attempt * 2));
                logger.LogWarning(
                    ex,
                    "Kafka broker not ready at {BootstrapServers} (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                    _options.BootstrapServers,
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }
    }
}

