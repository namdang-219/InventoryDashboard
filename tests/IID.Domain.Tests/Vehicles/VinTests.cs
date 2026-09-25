using FluentAssertions;
using IID.Domain.Vehicles;

namespace IID.Domain.Tests.Vehicles;

public class VinTests
{
    [Theory]
    [InlineData("1HGBH41JXMN109186")]
    [InlineData("JH4DA1750HS000001")]
    [InlineData("5YJ3E1EA0JF000000")]
    public void Parse_Should_AcceptValidVin(string raw)
    {
        var vin = Vin.Parse(raw);

        vin.Value.Should().Be(raw.ToUpperInvariant());
    }

    [Theory]
    [InlineData("1HGBH41JXMN10918")]      // too short
    [InlineData("1HGBH41JXMN1091866")]    // too long
    [InlineData("1HGBH41JXMN10918I")]     // I not allowed
    [InlineData("1HGBH41JXMN10918O")]     // O not allowed
    [InlineData("1HGBH41JXMN10918Q")]     // Q not allowed
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Should_ThrowForInvalidVin(string raw)
    {
        var act = () => Vin.Parse(raw);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToString_Should_ReturnValue()
    {
        var vin = Vin.Parse("1HGBH41JXMN109186");

        vin.ToString().Should().Be("1HGBH41JXMN109186");
    }
}
