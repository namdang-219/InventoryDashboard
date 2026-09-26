using FluentAssertions;
using IID.Domain.Vehicles;

namespace IID.Domain.Tests.Vehicles;

public class MoneyTests
{
    [Fact]
    public void Of_Should_CreateValidMoney()
    {
        var money = Money.Of(100.50m, "usd");

        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Of_Should_DefaultToUsd()
    {
        var money = Money.Of(50m);

        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Of_Should_ThrowForNegativeAmount()
    {
        var act = () => Money.Of(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("US")]
    [InlineData("USDX")]
    public void Of_Should_ThrowForInvalidCurrency(string currency)
    {
        var act = () => Money.Of(100m, currency);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Zero_Should_ReturnZeroAmount()
    {
        var money = Money.Zero("eur");

        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Money_Should_HaveValueEquality()
    {
        var m1 = Money.Of(100m, "USD");
        var m2 = Money.Of(100m, "USD");
        var m3 = Money.Of(200m, "USD");

        (m1 == m2).Should().BeTrue();
        m1.Should().Be(m2);
        (m1 != m3).Should().BeTrue();
    }
}
