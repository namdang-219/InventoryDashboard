using IID.Application.Common.Messaging;

namespace IID.Infrastructure.Configuration;

public sealed class KafkaConfiguration
{
    public const string Name = "Kafka";

    public bool Enabled { get; set; } = true;
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string VehicleMutationsTopic { get; set; } = KafkaTopics.VehicleMutations;
    public string DashboardUpdatesTopic { get; set; } = KafkaTopics.DashboardUpdates;
    public string ReportGeneratorGroupId { get; set; } = "iid-report-generator";
    public string ApiGroupId { get; set; } = "iid-api-realtime";
    public int DebounceWindowMs { get; set; } = 200;
    public int TopicPartitions { get; set; } = 3;
    public short TopicReplicationFactor { get; set; } = 1;
    public int MessageTimeoutMs { get; set; } = 5000;
}

