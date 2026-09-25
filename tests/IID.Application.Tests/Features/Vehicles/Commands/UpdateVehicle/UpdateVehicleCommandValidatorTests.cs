using FluentValidation.TestHelper;
using IID.Application.Vehicles.Commands.UpdateVehicle;
using IID.Domain.Vehicles;
using Xunit;

namespace IID.Application.Tests.Features.Vehicles.Commands.UpdateVehicle;

public class UpdateVehicleCommandValidatorTests
{
    private readonly UpdateVehicleCommandValidator _validator = new();

    private static UpdateVehicleCommand CreateValid() =>
        new(
            Guid.NewGuid(),
            "Toyota",
            "Camry",
            2022,
            "Silver",
            15000,
            FuelType.Petrol,
            20000m,
            24000m,
            VehicleStatus.Available);

    [Fact]
    public void Validate_Should_Pass_WhenValid()
    {
        _validator.TestValidate(CreateValid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_WhenIdEmpty()
    {
        var cmd = CreateValid() with { Id = Guid.Empty };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Validate_Should_Fail_WhenMakeOrModelEmpty()
    {
        var cmd = CreateValid() with { Make = "", Model = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.Make);
        result.ShouldHaveValidationErrorFor(c => c.Model);
    }

    [Fact]
    public void Validate_Should_Fail_WhenPricesInvalid()
    {
        var cmd = CreateValid() with { PurchasePrice = 0, AskingPrice = -100 };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.PurchasePrice);
        result.ShouldHaveValidationErrorFor(c => c.AskingPrice);
    }

    [Fact]
    public void Validate_Should_Fail_WhenMileageNegative()
    {
        var cmd = CreateValid() with { Mileage = -5 };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Mileage);
    }
}
