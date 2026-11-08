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

public class VehicleMutationConsumerProcessBatchTests
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
    public async Task ProcessBatch_Succeeds_RefreshesOnce_NoDlq()
    {
        var batch = new[] { Evt(Guid.NewGuid()), Evt(Guid.NewGuid()) };
        _refresher.Setup(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        await CreateSut().ProcessBatchWithRetryAsync(batch, CancellationToken.None);

        _refresher.Verify(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()), Times.Once);
        _producer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessBatch_TransientFailure_RetriesThenSucceeds()
    {
        var batch = new[] { Evt(Guid.NewGuid()) };
        _refresher.SetupSequence(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"))
            .ReturnsAsync(1);

        await CreateSut().ProcessBatchWithRetryAsync(batch, CancellationToken.None);

        _refresher.Verify(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _producer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessBatch_PersistentFailure_RoutesToDlq_AfterMaxRetries()
    {
        var dealershipId = Guid.NewGuid();
        var batch = new[] { Evt(null), Evt(Guid.Empty), Evt(dealershipId) };
        _refresher.Setup(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        VehicleMutationDeadLetterIntegrationEvent? sent = null;
        _producer.Setup(p => p.PublishAsync(
                KafkaTopics.VehicleMutationsDlq,
                dealershipId.ToString("D"),
                It.IsAny<VehicleMutationDeadLetterIntegrationEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, VehicleMutationDeadLetterIntegrationEvent, CancellationToken>((_, _, m, _) => sent = m)
            .Returns(Task.CompletedTask);

        await CreateSut().ProcessBatchWithRetryAsync(batch, CancellationToken.None);

        // 1 initial attempt + 2 retries
        _refresher.Verify(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()), Times.Exactly(3));
        sent.Should().NotBeNull();
        sent!.Mutations.Should().BeEquivalentTo(batch);
        sent.RetryCount.Should().Be(2);
        sent.ErrorType.Should().Be(typeof(InvalidOperationException).FullName);
        sent.ErrorMessage.Should().Be("db down");
        sent.SourceTopic.Should().Be(KafkaTopics.VehicleMutations);
    }

    [Fact]
    public async Task ProcessBatch_NoDealershipInBatch_UsesGlobalDlqKey()
    {
        var batch = new[] { Evt(null) };
        _refresher.Setup(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("x"));

        await CreateSut(new KafkaConfiguration { MutationMaxRetryCount = 0 })
            .ProcessBatchWithRetryAsync(batch, CancellationToken.None);

        _refresher.Verify(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()), Times.Once);
        _producer.Verify(p => p.PublishAsync(
            KafkaTopics.VehicleMutationsDlq,
            DashboardSnapshot.GlobalScopeKey,
            It.IsAny<VehicleMutationDeadLetterIntegrationEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatch_NegativeRetryConfig_IsClampedToZero()
    {
        var batch = new[] { Evt(null) };
        _refresher.Setup(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("x"));

        await CreateSut(new KafkaConfiguration { MutationMaxRetryCount = -5, MutationRetryDelayMs = -1 })
            .ProcessBatchWithRetryAsync(batch, CancellationToken.None);

        _refresher.Verify(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()), Times.Once);
        _producer.Verify(p => p.PublishAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<VehicleMutationDeadLetterIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatch_Cancelled_DoesNotRetryOrDeadLetter()
    {
        var batch = new[] { Evt(Guid.NewGuid()) };
        using var cts = new CancellationTokenSource();
        _refresher.Setup(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()))
            .Returns<IReadOnlyCollection<VehicleMutatedIntegrationEvent>, CancellationToken>((_, _) =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });

        var act = () => CreateSut().ProcessBatchWithRetryAsync(batch, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _refresher.Verify(r => r.RefreshForMutationsAsync(batch, It.IsAny<CancellationToken>()), Times.Once);
        _producer.VerifyNoOtherCalls();
    }
}
