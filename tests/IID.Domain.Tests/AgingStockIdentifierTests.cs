using IID.Domain;
using IID.Domain.Common;
using IID.Domain.Vehicles;
using Xunit;

namespace IID.Domain.Tests;

public class AgingStockIdentifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Flags_aging_when_over_90_days()
    {
        var v = Vehicle.Create(
            Vin.Parse("5YJ3E1EA7JF000001"), "Tesla", "Model 3", 2018, "Red",
            10_000, FuelType.Electric,
            Money.Of(30_000m), Money.Of(35_000m),
            VehicleStatus.Available,
            Now.AddDays(-91), Now);
        var sut = new AgingStockIdentifier(Now);
        Assert.True(sut.IsAging(v));
        Assert.Equal(91, sut.DaysInInventory(v));
    }

    [Fact]
    public void Not_aging_when_under_90_days()
    {
        var v = Vehicle.Create(
            Vin.Parse("5YJ3E1EA7JF000002"), "Tesla", "Model 3", 2024, "Black",
            5_000, FuelType.Electric,
            Money.Of(40_000m), Money.Of(45_000m),
            VehicleStatus.Available,
            Now.AddDays(-30), Now);
        var sut = new AgingStockIdentifier(Now);
        Assert.False(sut.IsAging(v));
    }
}

public class VinTests
{
    [Theory]
    [InlineData("5YJ3E1EA7JF000001")]
    [InlineData("JH4DA1760HS000001")]
    public void Accepts_valid_vin(string raw)
    {
        var vin = Vin.Parse(raw);
        Assert.Equal(raw.Length, vin.Value.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("5YJ3E1EA7JF00000I")]   // 'I' forbidden in ISO 3779
    [InlineData("5YJ3E1EA7JF00000")]    // 16 chars
    [InlineData("5YJ3E1EA7JF0000012")]  // 18 chars
    public void Rejects_invalid_vin(string raw)
    {
        Assert.Throws<ArgumentException>(() => Vin.Parse(raw));
    }
}

public class MoneyTests
{
    [Fact]
    public void Negative_amount_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Of(-1m));
    }

    [Fact]
    public void Bad_currency_throws()
    {
        Assert.Throws<ArgumentException>(() => Money.Of(10m, "US"));
    }
}
