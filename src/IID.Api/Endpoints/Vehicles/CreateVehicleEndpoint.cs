using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.Vehicles.Commands.CreateVehicle;
using IID.Domain.Vehicles;
using MediatR;

namespace IID.Api.Endpoints.Vehicles;

/// <summary>
/// POST /api/v1/vehicles — creates a new vehicle.
/// </summary>
public sealed class CreateVehicleEndpoint(ISender sender) : Endpoint<CreateVehicleRequest, object>
{
    public override void Configure()
    {
        Post("/api/v1/vehicles");
        Roles("Manager");
        Description(x => x.WithTags("Vehicles"));
        Summary(s =>
        {
            s.Summary = "Create a new vehicle";
            s.Description = "Creates a new vehicle in the inventory after validating VIN format, pricing, and required fields.";
            s.Response(200, "Vehicle created successfully.");
            s.Response(400, "Validation failed or VIN already exists.");
            s.Response(401, "Unauthorized.");
            s.Response(403, "Forbidden.");
        });
    }

    public override async Task HandleAsync(CreateVehicleRequest req, CancellationToken ct)
    {
        var cmd = new CreateVehicleCommand(
            req.Vin, req.Make, req.Model, req.Year, req.Color,
            req.Mileage,
            Enum.TryParse<FuelType>(req.FuelType, ignoreCase: true, out var ft) ? ft : FuelType.Petrol,
            req.PurchasePrice, req.AskingPrice,
            Enum.Parse<VehicleStatus>(req.Status),
            req.DateAddedToInventory,
            req.StockNumber,
            req.DealershipId);

        var result = await sender.Send(cmd, ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to create vehicle.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }
        await Send.OkAsync(new { id = result.Value }, cancellation: ct);
    }
}

public sealed class CreateVehicleRequest
{
    public Guid? DealershipId { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string? StockNumber { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public string FuelType { get; set; } = "Petrol";
    public decimal PurchasePrice { get; set; }
    public decimal AskingPrice { get; set; }
    public string Status { get; set; } = "Available";
    public DateTimeOffset DateAddedToInventory { get; set; }
}
