using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Dashboard.Dtos;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Application.Features.Dashboard.Services;
using IID.Domain.Dealerships;
using IID.Domain.Vehicles;
using IID.Infrastructure.Persistence;
using IID.Infrastructure.Persistence.Repositories;
using IID.Infrastructure.Realtime;
using IID.Infrastructure.Realtime.Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace IID.Infrastructure.Tests.Persistence;

public class DashboardSnapshotRepositoryAndScopingTests
{
    private static IidDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IidDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IidDbContext(options);
    }

    private static DashboardSnapshotPayload BuildPayload(Guid? dealershipId, int totalInventory, DateTimeOffset now)
    {
        var scopeKey = IID.Domain.Dashboard.DashboardSnapshot.BuildScopeKey(dealershipId);
        return new DashboardSnapshotPayload(
            dealershipId,
            scopeKey,
            new DashboardSummaryDto(now, totalInventory, totalInventory, 0, 0, 0, 0, [], totalInventory * 10_000m, 10_000m, 15, [], [], []),
            new QuickStatsDto([]),
            new InventoryChartsDto([], [], [], []),
            [],
            [],
            [new LowInventoryAlertDto(Guid.NewGuid(), "Toyota", "Camry", 1, 3)],
            [],
            now);
    }

    [Fact]
    public async Task UpsertAsync_Should_InsertVersion1AndIncrementVersionOnSubsequentUpsert()
    {
        using var db = CreateDbContext();
        var repo = new DashboardSnapshotRepository(db);
        var now = DateTimeOffset.UtcNow;

        var v1 = await repo.UpsertAsync(BuildPayload(null, 10, now), CancellationToken.None);
        v1.ScopeKey.Should().Be("global");
        v1.Version.Should().Be(1);

        var v2 = await repo.UpsertAsync(BuildPayload(null, 25, now.AddMinutes(1)), CancellationToken.None);
        v2.ScopeKey.Should().Be("global");
        v2.Version.Should().Be(2);

        var fetched = await repo.GetByScopeAsync("global", CancellationToken.None);
        fetched.Should().NotBeNull();
        fetched!.Version.Should().Be(2);
        DashboardSnapshotSerializer.ReadSummary(fetched).TotalInventory.Should().Be(25);
        DashboardSnapshotSerializer.ReadLowInventoryAlerts(fetched).Should().ContainSingle(a => a.Make == "Toyota");
    }

    [Fact]
    public async Task ListAgingAsync_Should_IncludeBothHighAndCriticalActiveVehiclesAndExcludeSold()
    {
        using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var dealer = Dealership.Create("Main", "MAIN", "Dallas", "TX", "555-1234", now);
        db.Dealerships.Add(dealer);

        var fresh = Vehicle.Create(Vin.Parse("1HGBH41JXMN109181"), "Honda", "Civic", 2024, "Red", 5000, FuelType.Petrol, Money.Of(18000m), Money.Of(21000m), VehicleStatus.Available, now.AddDays(-20), now, dealershipId: dealer.Id);
        var high = Vehicle.Create(Vin.Parse("1HGBH41JXMN109182"), "Honda", "Accord", 2023, "Blue", 15000, FuelType.Petrol, Money.Of(22000m), Money.Of(25000m), VehicleStatus.Available, now.AddDays(-70), now, dealershipId: dealer.Id);
        var critical = Vehicle.Create(Vin.Parse("1HGBH41JXMN109183"), "Honda", "Pilot", 2022, "Black", 25000, FuelType.Petrol, Money.Of(30000m), Money.Of(34000m), VehicleStatus.Wholesale, now.AddDays(-150), now, dealershipId: dealer.Id);
        var soldOld = Vehicle.Create(Vin.Parse("1HGBH41JXMN109184"), "Honda", "CR-V", 2022, "White", 20000, FuelType.Petrol, Money.Of(24000m), Money.Of(28000m), VehicleStatus.Available, now.AddDays(-180), now, dealershipId: dealer.Id);
        soldOld.MarkSold(Money.Of(27500m), now.AddDays(-5), "saler");

        await db.Vehicles.AddRangeAsync(fresh, high, critical, soldOld);
        await db.SaveChangesAsync();

        var repo = new VehicleRepository(db);
        var (items, total) = await repo.ListAgingAsync(minAgeDays: 60, page: 1, limit: 20, dealershipId: dealer.Id, asOfUtc: now);

        total.Should().Be(2);
        items.Select(x => x.Id).Should().ContainInOrder(critical.Id, high.Id); // oldest first
    }

    [Fact]
    public async Task SignalRVehicleHubNotifier_Should_RouteDashboardBroadcastsByDealershipOrGlobalGroup()
    {
        var hubContext = new Mock<IHubContext<InventoryHub, IInventoryClient>>();
        var clients = new Mock<IHubClients<IInventoryClient>>();
        var globalClient = new Mock<IInventoryClient>();
        var branchClient = new Mock<IInventoryClient>();

        var branchId = Guid.NewGuid();
        clients.Setup(c => c.Group(InventoryHubConstants.DashboardGroup)).Returns(globalClient.Object);
        clients.Setup(c => c.Group(InventoryHubConstants.DealershipGroup(branchId))).Returns(branchClient.Object);
        hubContext.SetupGet(h => h.Clients).Returns(clients.Object);

        var notifier = new SignalRVehicleHubNotifier(hubContext.Object, new RealtimeContractMapper());
        var summary = new DashboardSummaryDto(DateTimeOffset.UtcNow, 10, 8, 1, 1, 0, 2, [], 200_000m, 22_000m, 25, [], [], []);

        // Scoped to Branch
        await notifier.DashboardSummaryUpdatedAsync(summary, CancellationToken.None, branchId);
        branchClient.Verify(c => c.DashboardSummaryUpdated(It.IsAny<DashboardSummaryRealtimeDto>()), Times.Once);
        globalClient.Verify(c => c.DashboardSummaryUpdated(It.IsAny<DashboardSummaryRealtimeDto>()), Times.Never);

        // Scoped to Global ("All Dealerships")
        await notifier.DashboardSummaryUpdatedAsync(summary, CancellationToken.None, dealershipId: null);
        globalClient.Verify(c => c.DashboardSummaryUpdated(It.IsAny<DashboardSummaryRealtimeDto>()), Times.Once);
    }
}

