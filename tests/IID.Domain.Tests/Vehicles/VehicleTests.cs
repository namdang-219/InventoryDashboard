using FluentAssertions;
using IID.Domain.Common;
using IID.Domain.Vehicles;
using IID.Domain.Vehicles.Events;

namespace IID.Domain.Tests.Vehicles;

public class VehicleTests
{
    private static Vehicle CreateSample(DateTimeOffset? now = null, string? stockNumber = null) =>
        Vehicle.Create(
            vin: Vin.Parse("1HGBH41JXMN109186"),
            make: "Honda",
            model: "Civic",
            year: 2023,
            color: "Red",
            mileage: 15000,
            fuelType: FuelType.Petrol,
            purchasePrice: Money.Of(20000m),
            askingPrice: Money.Of(25000m),
            status: VehicleStatus.Available,
            dateAddedToInventory: now?.AddDays(-30) ?? DateTimeOffset.UtcNow.AddDays(-30),
            nowUtc: now ?? DateTimeOffset.UtcNow,
            createdByUserId: "user-1",
            stockNumber: stockNumber);

    [Fact]
    public void Create_Should_PopulateProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var v = CreateSample(now);

        v.Id.Should().NotBe(Guid.Empty);
        v.Make.Should().Be("Honda");
        v.Model.Should().Be("Civic");
        v.Year.Should().Be(2023);
        v.Status.Should().Be(VehicleStatus.Available);
        v.CreatedAt.Should().Be(now);
        v.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Create_Should_TrimMakeModelColor()
    {
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "  Honda  ", "  Civic  ", 2023, "  Red  ", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        v.Make.Should().Be("Honda");
        v.Model.Should().Be("Civic");
        v.Color.Should().Be("Red");
    }

    [Fact]
    public void Create_Should_GenerateStockNumber_WhenMissing()
    {
        var v = CreateSample();

        v.StockNumber.Should().StartWith("STK-");
    }

    [Fact]
    public void Create_Should_UseProvidedStockNumber()
    {
        var v = CreateSample(stockNumber: "STK-CUSTOM");

        v.StockNumber.Should().Be("STK-CUSTOM");
    }

    [Fact]
    public void Create_Should_RaiseVehicleAddedEvent()
    {
        var v = CreateSample();

        v.DomainEvents.Should().HaveCount(1);
        v.DomainEvents.First().Should().BeOfType<VehicleAdded>();
    }

    [Theory]
    [InlineData("", "Civic", 2023, "Red", 0)]
    [InlineData("Honda", "", 2023, "Red", 0)]
    [InlineData("Honda", "Civic", 1979, "Red", 0)]
    [InlineData("Honda", "Civic", 2023, "", 0)]
    [InlineData("Honda", "Civic", 2023, "Red", -1)]
    public void Create_Should_ThrowForInvalidInputs(string make, string model, int year, string color, int mileage)
    {
        var now = DateTimeOffset.UtcNow;
        var act = () => Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            make, model, year, color, mileage,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, now, now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_Throw_WhenDateAddedInFuture()
    {
        var now = DateTimeOffset.UtcNow;
        var future = now.AddDays(1);
        var act = () => Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, future, now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_Should_Throw_WhenSold()
    {
        var v = CreateSample();
        v.MarkSold(Money.Of(25000m), DateTimeOffset.UtcNow, "user-1");

        var act = () => v.Update("X", "Y", 2023, "Z", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Sold, DateTimeOffset.UtcNow, "user-1");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_Should_RaiseEvent_OnStatusChange()
    {
        var v = CreateSample();
        v.ClearDomainEvents();

        v.Update("Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Pending, DateTimeOffset.UtcNow, "user-1");

        v.DomainEvents.OfType<VehicleStatusChanged>().Should().HaveCount(1);
    }

    [Fact]
    public void MarkSold_Should_SetSoldPrice()
    {
        var v = CreateSample();
        v.MarkSold(Money.Of(26000m), DateTimeOffset.UtcNow, "user-1");

        v.Status.Should().Be(VehicleStatus.Sold);
        v.SoldPrice!.Amount.Should().Be(26000m);
        v.SoldAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkSold_Should_BeIdempotent()
    {
        var v = CreateSample();
        var firstSoldAt = DateTimeOffset.UtcNow;
        v.MarkSold(Money.Of(26000m), firstSoldAt, "user-1");
        v.MarkSold(Money.Of(99999m), DateTimeOffset.UtcNow, "user-1");

        v.SoldPrice!.Amount.Should().Be(26000m);
    }

    [Fact]
    public void MarkSold_Should_Throw_WhenWholesale()
    {
        var v = CreateSample();
        v.Update("Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Wholesale, DateTimeOffset.UtcNow, "user-1");

        var act = () => v.MarkSold(Money.Of(1m), DateTimeOffset.UtcNow, "user-1");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SoftDelete_Should_SetDeletedAt()
    {
        var v = CreateSample();
        var now = DateTimeOffset.UtcNow;
        v.SoftDelete(now, "user-1");

        v.DeletedAt.Should().Be(now);
    }

    [Fact]
    public void SoftDelete_Should_BeIdempotent()
    {
        var v = CreateSample();
        var first = DateTimeOffset.UtcNow;
        v.SoftDelete(first, "user-1");
        v.SoftDelete(DateTimeOffset.UtcNow, "user-1");

        v.DeletedAt.Should().Be(first);
    }

    [Fact]
    public void DaysInInventory_Should_BeZero_WhenAddedToday()
    {
        var now = DateTimeOffset.UtcNow;
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, now, now);

        v.DaysInInventory(now).Should().Be(0);
    }

    [Fact]
    public void GrossProfit_Should_BeNull_WhenNotSold()
    {
        var v = CreateSample();
        v.GrossProfit.Should().BeNull();
    }

    [Fact]
    public void GrossProfit_Should_BeSoldPriceMinusPurchase()
    {
        var v = CreateSample();
        v.MarkSold(Money.Of(26000m), DateTimeOffset.UtcNow, "user-1");

        v.GrossProfit!.Amount.Should().Be(6000m);
    }

    [Fact]
    public void GetAgingSeverity_Should_BeCritical_After90Days()
    {
        var now = DateTimeOffset.UtcNow;
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, now.AddDays(-100), now);

        v.GetAgingSeverity(now).Should().Be(AgingSeverity.Critical);
    }

    [Fact]
    public void GetDemandScore_Should_BeHigher_ForEvNewVehicle()
    {
        var now = DateTimeOffset.UtcNow;
        var ev = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Tesla", "Model 3", now.Year, "White", 1000,
            FuelType.Electric, Money.Of(40000m), Money.Of(45000m),
            VehicleStatus.Available, now, now);
        var petrol = Vehicle.Create(
            Vin.Parse("JH4DA1750HS000001"),
            "Toyota", "Corolla", 2020, "White", 50000,
            FuelType.Petrol, Money.Of(15000m), Money.Of(18000m),
            VehicleStatus.Available, now, now);

        ev.GetDemandScore(now).Should().BeGreaterThan(petrol.GetDemandScore(now));
    }

    [Fact]
    public void GetDemandScore_Should_BeZero_WhenSold()
    {
        var v = CreateSample();
        v.MarkSold(Money.Of(26000m), DateTimeOffset.UtcNow, "user-1");

        v.GetDemandScore().Should().Be(0);
    }

    [Theory]
    [InlineData(80, DemandLevel.High)]
    [InlineData(70, DemandLevel.High)]
    [InlineData(50, DemandLevel.Medium)]
    [InlineData(40, DemandLevel.Medium)]
    [InlineData(20, DemandLevel.Low)]
    public void GetDemandLevel_Should_BucketByScore(int scoreTarget, DemandLevel expected)
    {
        // Use the public Vehicle API to force a particular demand bucket by
        // controlling fuel type and age. EV + new year + fresh ⇒ high.
        var now = DateTimeOffset.UtcNow;
        var (fuel, year, daysOld) = scoreTarget switch
        {
            >= 70 => (FuelType.Electric, now.Year, 0),
            >= 40 => (FuelType.Hybrid, now.Year - 2, 60),
            _ => (FuelType.Diesel, now.Year - 10, 200)
        };
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Make", "Model", year, "Red", 0,
            fuel, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, now.AddDays(-daysOld), now);

        v.GetDemandLevel(now).Should().Be(expected);
    }

    [Fact]
    public void GetDemandLevel_Should_BeLow_WhenSold()
    {
        var v = CreateSample();
        v.MarkSold(Money.Of(26000m), DateTimeOffset.UtcNow, "user-1");

        v.GetDemandLevel().Should().Be(DemandLevel.Low);
    }

    [Fact]
    public void GetAgingSeverity_Should_BeNone_WhenSold()
    {
        var v = CreateSample();
        v.MarkSold(Money.Of(26000m), DateTimeOffset.UtcNow, "user-1");

        v.GetAgingSeverity().Should().Be(AgingSeverity.None);
    }

    [Theory]
    [InlineData(10, AgingSeverity.None)]
    [InlineData(30, AgingSeverity.Warning)]
    [InlineData(60, AgingSeverity.High)]
    [InlineData(89, AgingSeverity.High)]
    [InlineData(90, AgingSeverity.Critical)]
    public void GetAgingSeverity_Should_BucketByDaysCorrectly(int daysOld, AgingSeverity expected)
    {
        var now = DateTimeOffset.UtcNow;
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, now.AddDays(-daysOld), now);

        v.GetAgingSeverity(now).Should().Be(expected);
    }

    [Fact]
    public void IsAging_Should_DefaultTo90Days()
    {
        var now = DateTimeOffset.UtcNow;
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, now.AddDays(-91), now);

        v.IsAging().Should().BeTrue();
        v.IsAging(200).Should().BeFalse();
    }

    [Fact]
    public void DaysInInventory_Should_FreezeAtSoldAt_WhenSold()
    {
        var now = DateTimeOffset.UtcNow;
        var addedAt = now.AddDays(-30);
        var soldAt = now.AddDays(-10);

        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, addedAt, addedAt);
        v.MarkSold(Money.Of(2m), soldAt, "user-1");

        // Frozen at SoldAt - DateAdded, regardless of how much time has since passed
        v.DaysInInventory().Should().Be(20);
    }

    [Fact]
    public void DaysInInventory_Should_NeverReturnNegative()
    {
        var now = DateTimeOffset.UtcNow;
        // Clock-skew scenario: DateAdded is in the "future" relative to asOf
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, now, now);

        v.DaysInInventory(now.AddDays(-5)).Should().Be(0);
    }

    [Fact]
    public void GrossMarginPercent_Should_BeNull_WhenNotSold()
    {
        var v = CreateSample();
        v.GrossMarginPercent.Should().BeNull();
    }

    [Fact]
    public void GrossMarginPercent_Should_ComputeSoldOverPurchase()
    {
        var now = DateTimeOffset.UtcNow;
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(10000m), Money.Of(15000m),
            VehicleStatus.Available, now, now);
        v.MarkSold(Money.Of(12500m), DateTimeOffset.UtcNow, "user-1");

        // (12500 - 10000) / 10000 * 100 = 25.00
        v.GrossMarginPercent.Should().Be(25.00m);
    }

    [Fact]
    public void GrossMarginPercent_Should_BeNull_WhenPurchasePriceZero()
    {
        var now = DateTimeOffset.UtcNow;
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(0m), Money.Of(0m),
            VehicleStatus.Available, now, now);
        v.MarkSold(Money.Of(1m), DateTimeOffset.UtcNow, "user-1");

        v.GrossMarginPercent.Should().BeNull();
    }

    [Fact]
    public void Update_Should_RaiseVehicleUpdatedEvent()
    {
        var v = CreateSample();
        v.ClearDomainEvents();

        v.Update("Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, DateTimeOffset.UtcNow, "user-1");

        v.DomainEvents.OfType<VehicleUpdated>().Should().HaveCount(1);
    }

    [Fact]
    public void Update_Should_NotRaiseStatusChanged_WhenStatusUnchanged()
    {
        var v = CreateSample();
        v.ClearDomainEvents();

        v.Update("Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, DateTimeOffset.UtcNow, "user-1");

        v.DomainEvents.OfType<VehicleStatusChanged>().Should().BeEmpty();
    }

    [Fact]
    public void Update_Should_Throw_WhenNowBeforeDateAdded()
    {
        // Update validates that DateAddedToInventory <= nowUtc. If the caller's
        // clock is somehow behind the original DateAdded (e.g. timezone drift),
        // Update must reject the call rather than corrupt the aggregate.
        var originalNow = DateTimeOffset.UtcNow;
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, originalNow, originalNow);

        var act = () => v.Update("Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, originalNow.AddDays(-1), "user-1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SoftDelete_Should_RaiseVehicleRemovedEvent()
    {
        var v = CreateSample();
        v.ClearDomainEvents();

        v.SoftDelete(DateTimeOffset.UtcNow, "user-1");

        v.DomainEvents.OfType<VehicleRemoved>().Should().HaveCount(1);
    }

    [Fact]
    public void MarkSold_Should_RaiseVehicleSoldEvent_WithDaysToSell()
    {
        var now = DateTimeOffset.UtcNow;
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, now.AddDays(-15), now);
        v.ClearDomainEvents();

        v.MarkSold(Money.Of(2m), now, "user-1");

        var sold = v.DomainEvents.OfType<IID.Domain.Vehicles.Events.VehicleSold>().Single();
        sold.DaysToSell.Should().Be(15);
    }

    [Fact]
    public void TransferDealership_Should_UpdateDealershipId_AndRaiseEvent()
    {
        var originId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, now, now,
            dealershipId: originId);
        v.ClearDomainEvents();

        v.TransferDealership(targetId, now, "manager-1");

        v.DealershipId.Should().Be(targetId);
        var transferEvent = v.DomainEvents.OfType<IID.Domain.Vehicles.Events.VehicleUpdated>().Single();
        transferEvent.VehicleId.Should().Be(v.Id);
        transferEvent.Make.Should().Be(v.Make);
    }

    [Fact]
    public void TransferDealership_Should_Throw_WhenEmptyGuid()
    {
        var v = CreateSample();
        var act = () => v.TransferDealership(Guid.Empty, DateTimeOffset.UtcNow, "manager-1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TransferDealership_Should_Throw_WhenSameDealership()
    {
        var dealerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2m),
            VehicleStatus.Available, now, now,
            dealershipId: dealerId);

        var act = () => v.TransferDealership(dealerId, now, "manager-1");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TransferDealership_Should_Throw_WhenVehicleSold()
    {
        var originId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var v = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1000m), Money.Of(2000m),
            VehicleStatus.Available, now, now,
            dealershipId: originId);

        v.MarkSold(Money.Of(2000m), now, "manager-1");

        var act = () => v.TransferDealership(targetId, now, "manager-1");
        act.Should().Throw<InvalidOperationException>();
    }
}
