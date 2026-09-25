namespace IID.Application.Vehicles.Commands.CreateVehicle;

public sealed record CreateVehicleCommand(
    string Vin,
    string Make,
    string Model,
    int Year,
    string Color,
    int Mileage,
    FuelType FuelType,
    decimal PurchasePrice,
    decimal AskingPrice,
    VehicleStatus Status,
    DateTimeOffset DateAddedToInventory,
    string? StockNumber = null,
    Guid? DealershipId = null) : MediatR.IRequest<Domain.Common.Result<Guid>>;
