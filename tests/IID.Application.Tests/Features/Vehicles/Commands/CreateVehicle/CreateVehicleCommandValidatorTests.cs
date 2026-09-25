using FluentAssertions;
using FluentValidation.TestHelper;
using IID.Application.Vehicles.Commands.CreateVehicle;
using IID.Domain.Vehicles;

namespace IID.Application.Tests.Features.Vehicles.Commands.CreateVehicle;

public class CreateVehicleCommandValidatorTests
{
    private readonly CreateVehicleCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_WhenValid()
    {
        var cmd = new CreateVehicleCommand(
            "1HGBH41JXMN109186", "Honda", "Civic", 2023, "Red", 1000,
            FuelType.Petrol, 15000m, 18000m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1));

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_WhenVinEmpty()
    {
        var cmd = new CreateVehicleCommand(
            "", "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, 0m, 0m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1));

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.Vin);
    }

    [Theory]
    [InlineData("1HGBH41JXMN10918")]   // too short
    [InlineData("1HGBH41JXMN1091866")] // too long
    [InlineData("1HGBH41JXMN10918I")]  // I not allowed
    public void Validate_Should_Fail_WhenVinInvalid(string vin)
    {
        var cmd = new CreateVehicleCommand(
            vin, "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, 0m, 0m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1));

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.Vin);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ThisMakeNameIsWayTooLongToBeAcceptedByTheValidatorBecauseItExceedsFiftyCharacters")]
    public void Validate_Should_Fail_WhenMakeInvalid(string make)
    {
        var cmd = new CreateVehicleCommand(
            "1HGBH41JXMN109186", make, "Civic", 2023, "Red", 0,
            FuelType.Petrol, 0m, 0m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1));

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.Make);
    }

    [Fact]
    public void Validate_Should_Fail_WhenYearOutOfRange()
    {
        var cmd = new CreateVehicleCommand(
            "1HGBH41JXMN109186", "Honda", "Civic", 1899, "Red", 0,
            FuelType.Petrol, 0m, 0m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1));

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.Year);
    }

    [Fact]
    public void Validate_Should_Fail_WhenFutureDate()
    {
        var cmd = new CreateVehicleCommand(
            "1HGBH41JXMN109186", "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, 0m, 0m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(1));

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.DateAddedToInventory);
    }

    [Fact]
    public void Validate_Should_Fail_WhenNegativeMileage()
    {
        var cmd = new CreateVehicleCommand(
            "1HGBH41JXMN109186", "Honda", "Civic", 2023, "Red", -1,
            FuelType.Petrol, 0m, 0m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1));

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.Mileage);
    }

    [Fact]
    public void Validate_Should_Pass_WhenOptionalStockNumberOmitted()
    {
        var cmd = new CreateVehicleCommand(
            "1HGBH41JXMN109186", "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, 0m, 0m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1));

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(c => c.StockNumber);
    }
}
