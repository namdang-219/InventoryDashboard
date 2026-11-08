using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Common.Messaging;
using IID.Domain.Dashboard;
using IID.Infrastructure.Configuration;
using IID.ReportGenerator.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace IID.ReportGenerator.Tests.HostedServices;

public class VehicleMutationConsumerLifecycleTests
{
    private readonly Mock<IDashboardSnapshotRefresher> _refresher = new();
    private readonly Mock<IKafkaEventProducer> _producer = new();
    private readonly DashboardBootstrapReadiness _readiness = new();

    private VehicleMutationConsumerHostedService CreateSut(KafkaConfiguration? cfg = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _refresher.Object);
        var provider = services.BuildServiceProvider();

        cfg ??= new KafkaConfiguration { MutationMaxRetryCount = 2, MutationRetryDelayMs = 0 };

        return new VehicleMutationConsumerHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            _producer.Object,
            _readiness,
            Options.Create(cfg),
            NullLogger<VehicleMutationConsumerHostedService>.Instance);
    }

    private static VehicleMutatedIntegrationEvent Evt(Guid? dealershipId) =>
        new(Guid.NewGuid(), VehicleMutationTypes.Added, Guid.NewGuid(), dealershipId, null, DateTimeOffset.UtcNow);

    [Fact]
    public async Task Consumer_WaitsForReadiness_BeforeStarting()
    {
        var sut = CreateSut(new KafkaConfiguration { Enabled = false });

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        sut.ExecuteTask!.IsCompleted.Should().BeFalse();

        _readiness.MarkReady();
        await sut.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(2));
        await sut.StopAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData(false, "localhost:9092")]
    [InlineData(true, "")]
    [InlineData(true, "   ")]
    public async Task Consumer_StaysIdle_WhenKafkaDisabledOrNoBootstrapServers(bool enabled, string servers)
    {
        var sut = CreateSut(new KafkaConfiguration { Enabled = enabled, BootstrapServers = servers });
        _readiness.MarkReady();

        await sut.StartAsync(CancellationToken.None);
        await sut.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(2));
        await sut.StopAsync(CancellationToken.None);

        _refresher.VerifyNoOtherCalls();
        _producer.VerifyNoOtherCalls();
    }
}
