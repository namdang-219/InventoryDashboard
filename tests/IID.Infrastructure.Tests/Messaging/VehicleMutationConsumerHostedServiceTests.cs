using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Common.Messaging;
using IID.Domain.Dashboard;
using IID.Infrastructure.Configuration;
using IID.ReportGenerator.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IID.Infrastructure.Tests.Messaging;

public sealed class VehicleMutationConsumerHostedServiceTests
{
    private static VehicleMutatedIntegrationEvent SampleMutation(Guid? dealershipId = null) =>
        new(
            Guid.NewGuid(),
            VehicleMutationTypes.Updated,
            Guid.NewGuid(),
            dealershipId,
            null,
            DateTimeOffset.UtcNow);

    private static (
        VehicleMutationConsumerHostedService Sut,
        FakeDashboardSnapshotRefresher Refresher,
        FakeKafkaEventProducer Producer,
        RecordingLogger<VehicleMutationConsumerHostedService> Logger,
        ServiceProvider ServiceProvider)
        CreateHarness(
            Func<int, IReadOnlyCollection<VehicleMutatedIntegrationEvent>, CancellationToken, Task<int>> refreshBehavior,
            Exception? dlqPublishException = null,
            int maxRetries = 3)
    {
        var refresher = new FakeDashboardSnapshotRefresher(refreshBehavior);
        var services = new ServiceCollection();
        services.AddScoped<ScopedInstanceTracker>();
        services.AddScoped<IDashboardSnapshotRefresher>(sp =>
        {
            var tracker = sp.GetRequiredService<ScopedInstanceTracker>();
            return new ScopedRefresherDecorator(refresher, tracker.ScopeId);
        });

        var serviceProvider = services.BuildServiceProvider();
        var producer = new FakeKafkaEventProducer(dlqPublishException);
        var logger = new RecordingLogger<VehicleMutationConsumerHostedService>();
        var readiness = new DashboardBootstrapReadiness();
        var kafkaOptions = Options.Create(new KafkaConfiguration
        {
            Enabled = true,
            BootstrapServers = "localhost:9092",
            VehicleMutationsTopic = KafkaTopics.VehicleMutations,
            VehicleMutationsDlqTopic = KafkaTopics.VehicleMutationsDlq,
            MutationMaxRetryCount = maxRetries,
            MutationRetryDelayMs = 0 // zero delay for fast unit tests
        });

        var sut = new VehicleMutationConsumerHostedService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            producer,
            readiness,
            kafkaOptions,
            logger);

        return (sut, refresher, producer, logger, serviceProvider);
    }

    [Fact]
    public async Task ProcessBatchWithRetryAsync_Should_SucceedOnFirstAttempt_WithoutRetryOrDlq()
    {
        var dealerId = Guid.NewGuid();
        var (sut, refresher, producer, logger, sp) = CreateHarness((_, _, _) => Task.FromResult(2));
        await using var _ = sp;

        await sut.ProcessBatchWithRetryAsync([SampleMutation(dealerId)], CancellationToken.None);

        refresher.CallCount.Should().Be(1);
        producer.PublishedMessages.Should().BeEmpty();
        logger.Entries.Where(e => e.Level >= LogLevel.Warning).Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBatchWithRetryAsync_Should_RetryUpToThreeTimesAndSucceed_WhenThirdRetrySucceeds()
    {
        var dealerId = Guid.NewGuid();
        // Calls: 1 (initial) fails, 2 (retry #1) fails, 3 (retry #2) fails, 4 (retry #3) succeeds
        var (sut, refresher, producer, _, sp) = CreateHarness((callNumber, _, _) =>
        {
            if (callNumber <= 3)
                throw new InvalidOperationException($"Transient SQL timeout #{callNumber}");

            return Task.FromResult(2);
        });
        await using var _ = sp;

        await sut.ProcessBatchWithRetryAsync([SampleMutation(dealerId)], CancellationToken.None);

        refresher.CallCount.Should().Be(4);
        refresher.ObservedScopeIds.Distinct().Should().HaveCount(4, "each attempt should run in a fresh DI scope");
        producer.PublishedMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBatchWithRetryAsync_Should_PublishToDeadLetterQueueAndLogWarning_AfterThreeRetriesStillFail()
    {
        var dealerId = Guid.NewGuid();
        var batch = new[] { SampleMutation(dealerId), SampleMutation(dealerId) };

        var (sut, refresher, producer, logger, sp) = CreateHarness((callNumber, _, _) =>
            throw new InvalidOperationException($"Database deadlock on attempt {callNumber}"));
        await using var _ = sp;

        await sut.ProcessBatchWithRetryAsync(batch, CancellationToken.None);

        // 1 initial attempt + 3 retries = 4 invocations total
        refresher.CallCount.Should().Be(4);

        producer.PublishedMessages.Should().ContainSingle();
        var published = producer.PublishedMessages.Single();
        published.Topic.Should().Be(KafkaTopics.VehicleMutationsDlq);
        published.Key.Should().Be(dealerId.ToString("D"));

        var dlqPayload = published.Message.Should().BeOfType<VehicleMutationDeadLetterIntegrationEvent>().Subject;
        dlqPayload.SourceTopic.Should().Be(KafkaTopics.VehicleMutations);
        dlqPayload.RetryCount.Should().Be(3);
        dlqPayload.Mutations.Should().HaveCount(2);
        dlqPayload.ErrorType.Should().Contain(nameof(InvalidOperationException));
        dlqPayload.ErrorMessage.Should().Contain("Database deadlock on attempt 4");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains(KafkaTopics.VehicleMutationsDlq, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessBatchWithRetryAsync_Should_UseGlobalScopeKeyForDlq_WhenDealershipIdIsMissing()
    {
        var batch = new[] { SampleMutation(dealershipId: null) };

        var (sut, _, producer, _, sp) = CreateHarness((_, _, _) =>
            throw new InvalidOperationException("Persistent failure"));
        await using var _ = sp;

        await sut.ProcessBatchWithRetryAsync(batch, CancellationToken.None);

        producer.PublishedMessages.Should().ContainSingle()
            .Which.Key.Should().Be(DashboardSnapshot.GlobalScopeKey);
    }

    [Fact]
    public async Task ProcessBatchWithRetryAsync_Should_Rethrow_WhenDeadLetterQueuePublishItselfFails()
    {
        var batch = new[] { SampleMutation(Guid.NewGuid()) };

        var (sut, refresher, _, _, sp) = CreateHarness(
            (_, _, _) => throw new InvalidOperationException("DB down"),
            dlqPublishException: new InvalidOperationException("Kafka broker unreachable"));
        await using var _ = sp;

        var act = () => sut.ProcessBatchWithRetryAsync(batch, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Kafka broker unreachable*");
        refresher.CallCount.Should().Be(4);
    }

    [Fact]
    public async Task ProcessBatchWithRetryAsync_Should_PropagateCancellationImmediately_WithoutRetryOrDlq()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var (sut, refresher, producer, _, sp) = CreateHarness((_, _, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(1);
        });
        await using var _ = sp;

        var act = () => sut.ProcessBatchWithRetryAsync([SampleMutation(Guid.NewGuid())], cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        refresher.CallCount.Should().Be(1);
        producer.PublishedMessages.Should().BeEmpty();
    }

    private sealed class ScopedInstanceTracker
    {
        public Guid ScopeId { get; } = Guid.NewGuid();
    }

    private sealed class ScopedRefresherDecorator(FakeDashboardSnapshotRefresher inner, Guid scopeId) : IDashboardSnapshotRefresher
    {
        public Task<int> RefreshAllSnapshotsAsync(bool publishUpdates = false, CancellationToken ct = default) =>
            inner.RefreshAllSnapshotsAsync(publishUpdates, ct);

        public Task<int> RefreshForMutationsAsync(IReadOnlyCollection<VehicleMutatedIntegrationEvent> mutations, CancellationToken ct = default) =>
            inner.RefreshForMutationsScopedAsync(scopeId, mutations, ct);
    }

    private sealed class FakeDashboardSnapshotRefresher(
        Func<int, IReadOnlyCollection<VehicleMutatedIntegrationEvent>, CancellationToken, Task<int>> handler)
    {
        private int _callCount;
        public int CallCount => _callCount;
        public List<Guid> ObservedScopeIds { get; } = [];

        public Task<int> RefreshAllSnapshotsAsync(bool publishUpdates, CancellationToken ct) =>
            Task.FromResult(0);

        public Task<int> RefreshForMutationsScopedAsync(
            Guid scopeId,
            IReadOnlyCollection<VehicleMutatedIntegrationEvent> mutations,
            CancellationToken ct)
        {
            var current = Interlocked.Increment(ref _callCount);
            ObservedScopeIds.Add(scopeId);
            return handler(current, mutations, ct);
        }
    }

    private sealed class FakeKafkaEventProducer(Exception? throwOnPublish = null) : IKafkaEventProducer
    {
        public List<(string Topic, string Key, object Message)> PublishedMessages { get; } = [];

        public Task PublishAsync<TMessage>(string topic, string key, TMessage message, CancellationToken ct = default)
        {
            if (throwOnPublish is not null)
                throw throwOnPublish;

            PublishedMessages.Add((topic, key, message!));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }
}

