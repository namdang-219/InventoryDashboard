using FluentAssertions;
using FluentValidation.TestHelper;
using IID.Application.Vehicles.Commands.MarkVehicleSold;

namespace IID.Application.Tests.Features.Vehicles.Commands.MarkVehicleSold;

public class MarkVehicleSoldCommandValidatorTests
{
    private readonly MarkVehicleSoldCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_WhenValidWithRawRowVersion()
    {
        var cmd = new MarkVehicleSoldCommand(
            Guid.NewGuid(), 26000m,
            SoldPriceCurrency: "USD",
            RowVersion: new byte[] { 1, 2, 3, 4 });

        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_WhenValidWithBase64RowVersion()
    {
        var cmd = new MarkVehicleSoldCommand(
            Guid.NewGuid(), 26000m,
            SoldPriceCurrency: "USD",
            RowVersionBase64: Convert.ToBase64String(new byte[] { 9, 9 }));

        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_WhenVehicleIdEmpty()
    {
        var cmd = new MarkVehicleSoldCommand(
            Guid.Empty, 100m,
            RowVersionBase64: "abc=");

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.VehicleId);
    }

    [Fact]
    public void Validate_Should_Fail_WhenSoldPriceNegative()
    {
        var cmd = new MarkVehicleSoldCommand(
            Guid.NewGuid(), -1m,
            RowVersionBase64: "abc=");

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.SoldPrice);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDX")]
    public void Validate_Should_Fail_WhenCurrencyWrongLength(string currency)
    {
        var cmd = new MarkVehicleSoldCommand(
            Guid.NewGuid(), 100m,
            SoldPriceCurrency: currency,
            RowVersionBase64: "abc=");

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.SoldPriceCurrency);
    }

    [Fact]
    public void Validate_Should_Fail_WhenRowVersionMissing()
    {
        var cmd = new MarkVehicleSoldCommand(
            Guid.NewGuid(), 100m);

        _validator.TestValidate(cmd).IsValid.Should().BeFalse();
    }
}
