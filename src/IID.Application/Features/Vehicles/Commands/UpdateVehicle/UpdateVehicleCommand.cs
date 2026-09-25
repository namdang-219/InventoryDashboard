using System;
using MediatR;

namespace IID.Application.Vehicles.Commands.UpdateVehicle;

public sealed record UpdateVehicleCommand(
    Guid Id,
    string Make,
    string Model,
    int Year,
    string Color,
    int Mileage,
    FuelType FuelType,
    decimal PurchasePrice,
    decimal AskingPrice,
    VehicleStatus Status) : IRequest<Result<Guid>>;
