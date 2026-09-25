using FluentAssertions;
using IID.Application.Common.Models;
using IID.Domain.Common;
using IID.Domain.Dealerships;
using IID.Domain.Vehicles;
using IID.Infrastructure.Persistence;
using IID.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IID.Infrastructure.Tests.Persistence;

public class VehicleRepositoryAgingTests
{
    private static IidDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IidDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IidDbContext(options);
    }

    private static Dealership CreateDealership(IidDbContext db)
    {
        var dealer = Dealership.Create("Main Dealership", "MAIN", "Dallas", "TX", "555-1234", DateTimeOffset.UtcNow);
        db.Dealerships.Add(dealer);
        db.SaveChanges();
        return dealer;
    }

    private static Vehicle CreateVehicle(string vin, int daysAgo, Guid dealershipId)
    {
        var now = DateTimeOffset.UtcNow;
        var dateAdded = now.AddDays(-daysAgo);
        return Vehicle.Create(
            Vin.Parse(vin),
            "Toyota", "Camry", 2022, "Silver", 10000,
            FuelType.Petrol, Money.Of(20000m), Money.Of(25000m),
            VehicleStatus.Available, dateAdded, now, dealershipId: dealershipId);
    }

    [Fact]
    public async Task ListAsync_Should_OnlyReturnHighAgingVehicles_WhenMinAgeDaysIs60()
    {
        using var db = CreateDbContext();
        var dealer = CreateDealership(db);

        var v10d = CreateVehicle("1HGBH41JXMN109181", 10, dealer.Id);   // None (<30)
        var v45d = CreateVehicle("1HGBH41JXMN109182", 45, dealer.Id);   // Warning (30-59)
        var v62d = CreateVehicle("1HGBH41JXMN109183", 62, dealer.Id);   // High (60-89)
        var v87d = CreateVehicle("1HGBH41JXMN109184", 87, dealer.Id);   // High (60-89)
        var v262d = CreateVehicle("1HGBH41JXMN109185", 262, dealer.Id); // Critical (>=90)
        var v265d = CreateVehicle("1HGBH41JXMN109186", 265, dealer.Id); // Critical (>=90)

        await db.Vehicles.AddRangeAsync(v10d, v45d, v62d, v87d, v262d, v265d);
        await db.SaveChangesAsync();

        var repo = new VehicleRepository(db);

        // Act: Filter by minAgeDays = 60 without maxAgeDays
        var (items, total) = await repo.ListAsync(new VehicleListFilter(MinAgeDays: 60), CancellationToken.None);

        // Assert: Should ONLY include High aging (62d, 87d). Critical (262d, 265d) must be excluded!
        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Select(v => v.Vin.Value).Should().Contain([v62d.Vin.Value, v87d.Vin.Value]);
        items.Select(v => v.Vin.Value).Should().NotContain(v262d.Vin.Value);
        items.Select(v => v.Vin.Value).Should().NotContain(v265d.Vin.Value);
        items.Select(v => v.Vin.Value).Should().NotContain(v45d.Vin.Value);
        items.Select(v => v.Vin.Value).Should().NotContain(v10d.Vin.Value);
    }

    [Fact]
    public async Task ListAsync_Should_OnlyReturnWarningAgingVehicles_WhenMinAgeDaysIs30()
    {
        using var db = CreateDbContext();
        var dealer = CreateDealership(db);

        var v10d = CreateVehicle("1HGBH41JXMN109181", 10, dealer.Id);
        var v45d = CreateVehicle("1HGBH41JXMN109182", 45, dealer.Id);
        var v75d = CreateVehicle("1HGBH41JXMN109183", 75, dealer.Id);

        await db.Vehicles.AddRangeAsync(v10d, v45d, v75d);
        await db.SaveChangesAsync();

        var repo = new VehicleRepository(db);

        var (items, total) = await repo.ListAsync(new VehicleListFilter(MinAgeDays: 30), CancellationToken.None);

        total.Should().Be(1);
        items.Should().ContainSingle(v => v.Vin.Value == v45d.Vin.Value);
    }

    [Fact]
    public async Task ListAsync_Should_FilterByVin()
    {
        using var db = CreateDbContext();
        var dealer = CreateDealership(db);
        var v1 = CreateVehicle("1HGBH41JXMN109181", 10, dealer.Id);
        var v2 = CreateVehicle("5YJ3E1EB9RA001151", 20, dealer.Id);
        await db.Vehicles.AddRangeAsync(v1, v2);
        await db.SaveChangesAsync();

        var repo = new VehicleRepository(db);
        var (items, total) = await repo.ListAsync(new VehicleListFilter(Vin: "001151"), CancellationToken.None);

        total.Should().Be(1);
        items.Should().ContainSingle(v => v.Vin.Value == v2.Vin.Value);
    }

    [Fact]
    public async Task ListAsync_Should_FilterByStockNumber()
    {
        using var db = CreateDbContext();
        var dealer = CreateDealership(db);
        var v1 = CreateVehicle("1HGBH41JXMN109181", 10, dealer.Id);
        var v2 = CreateVehicle("5YJ3E1EB9RA001151", 20, dealer.Id);
        await db.Vehicles.AddRangeAsync(v1, v2);
        await db.SaveChangesAsync();

        var repo = new VehicleRepository(db);
        var targetStock = v1.StockNumber;
        var (items, total) = await repo.ListAsync(new VehicleListFilter(StockNumber: targetStock), CancellationToken.None);

        total.Should().Be(1);
        items.Should().ContainSingle(v => v.Id == v1.Id);
    }
}
