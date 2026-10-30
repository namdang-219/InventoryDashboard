using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Common.Messaging;
using IID.Application.Dashboard.Dtos;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Application.Features.Dashboard.Services;
using IID.Domain.Dashboard;
using IID.Domain.Dealerships;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.Dashboard.Services;

public class DashboardSnapshotRefresherTests
{
    private readonly Mock<IDealershipRepository> _dealerships = new();
    private readonly Mock<IDashboardSnapshotCalculator> _calculator = new();
    private readonly Mock<IDashboardSnapshotRepository> _snapshots = new();
    private readonly Mock<IKafkaEventProducer> _producer = new();
    private readonly Mock<IClock> _clock = new();

    private DashboardSnapshotRefresher CreateSut(DateTimeOffset now)
    {
        _clock.SetupGet(c => c.UtcNow).Returns(now);

        _calculator
            .Setup(c => c.CalculateForScopeAsync(It.IsAny<Guid?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid? dealerId, DateTimeOffset? asOf, CancellationToken _) =>
            {
                var ts = asOf ?? now;
                var scopeKey = DashboardSnapshot.BuildScopeKey(dealerId);
                return new DashboardSnapshotPayload(
                    dealerId,
                    scopeKey,
                    new DashboardSummaryDto(ts, 5, 3, 1, 1, 0, 1, [], 100_000m, 25_000m, 30, [], [], []),
                    new QuickStatsDto([]),
                    new InventoryChartsDto([], [], [], []),
                    [],
                    [],
                    [],
                    [],
                    ts);
            });

        _snapshots
            .Setup(s => s.UpsertAsync(It.IsAny<DashboardSnapshotPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardSnapshotPayload p, CancellationToken _) =>
                DashboardSnapshot.Create(p.DealershipId, "{}", "{}", "{}", "[]", "[]", "[]", "[]", p.ComputedAtUtc));

        return new DashboardSnapshotRefresher(
            _dealerships.Object,
            _calculator.Object,
            _snapshots.Object,
            _producer.Object,
            _clock.Object,
            NullLogger<DashboardSnapshotRefresher>.Instance);
    }

    [Fact]
    public async Task RefreshAllSnapshotsAsync_Should_UpsertGlobalAndEveryDealership()
    {
        var now = DateTimeOffset.UtcNow;
        var d1 = Dealership.Create("Branch A", "DLR-A", "Dallas", "TX", "555-0001", now);
        var d2 = Dealership.Create("Branch B", "DLR-B", "Austin", "TX", "555-0002", now);
        _dealerships.Setup(d => d.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([d1, d2]);

        var sut = CreateSut(now);
        var count = await sut.RefreshAllSnapshotsAsync(publishUpdates: true, CancellationToken.None);

        count.Should().Be(3); // global + d1 + d2
        _snapshots.Verify(s => s.UpsertAsync(It.Is<DashboardSnapshotPayload>(p => p.ScopeKey == "global"), It.IsAny<CancellationToken>()), Times.Once);
        _snapshots.Verify(s => s.UpsertAsync(It.Is<DashboardSnapshotPayload>(p => p.DealershipId == d1.Id), It.IsAny<CancellationToken>()), Times.Once);
        _snapshots.Verify(s => s.UpsertAsync(It.Is<DashboardSnapshotPayload>(p => p.DealershipId == d2.Id), It.IsAny<CancellationToken>()), Times.Once);
        _producer.Verify(p => p.PublishAsync(
            KafkaTopics.DashboardUpdates,
            It.IsAny<string>(),
            It.IsAny<DashboardSnapshotUpdatedIntegrationEvent>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task RefreshForMutationsAsync_Should_DeduplicateBurstForSameDealership()
    {
        var now = DateTimeOffset.UtcNow;
        var branchA = Guid.NewGuid();
        var sut = CreateSut(now);

        var burst = Enumerable.Range(0, 10)
            .Select(_ => new VehicleMutatedIntegrationEvent(
                Guid.NewGuid(),
                VehicleMutationTypes.Updated,
                Guid.NewGuid(),
                branchA,
                null,
                now))
            .ToList();

        var count = await sut.RefreshForMutationsAsync(burst, CancellationToken.None);

        count.Should().Be(2); // branchA once + global once
        _snapshots.Verify(s => s.UpsertAsync(It.IsAny<DashboardSnapshotPayload>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RefreshForMutationsAsync_Should_RegenerateBothBranchesAndGlobal_OnVehicleTransfer()
    {
        var now = DateTimeOffset.UtcNow;
        var sourceBranch = Guid.NewGuid();
        var targetBranch = Guid.NewGuid();
        var sut = CreateSut(now);

        var transfer = new VehicleMutatedIntegrationEvent(
            Guid.NewGuid(),
            VehicleMutationTypes.Transferred,
            Guid.NewGuid(),
            DealershipId: targetBranch,
            PreviousDealershipId: sourceBranch,
            OccurredAtUtc: now);

        var count = await sut.RefreshForMutationsAsync([transfer], CancellationToken.None);

        count.Should().Be(3); // sourceBranch + targetBranch + global
        _snapshots.Verify(s => s.UpsertAsync(It.Is<DashboardSnapshotPayload>(p => p.DealershipId == sourceBranch), It.IsAny<CancellationToken>()), Times.Once);
        _snapshots.Verify(s => s.UpsertAsync(It.Is<DashboardSnapshotPayload>(p => p.DealershipId == targetBranch), It.IsAny<CancellationToken>()), Times.Once);
        _snapshots.Verify(s => s.UpsertAsync(It.Is<DashboardSnapshotPayload>(p => p.ScopeKey == "global"), It.IsAny<CancellationToken>()), Times.Once);
    }
}

