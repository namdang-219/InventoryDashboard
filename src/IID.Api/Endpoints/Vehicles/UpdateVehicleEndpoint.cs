using FastEndpoints;
using IID.Application.Vehicles.Commands.UpdateVehicle;
using IID.Domain.Vehicles;
using MediatR;

namespace IID.Api.Endpoints.Vehicles;

public sealed record UpdateVehicleRequest(
    string Make,
    string Model,
    int Year,
    string Color,
    int Mileage,
    string FuelType,
    decimal PurchasePrice,
    decimal AskingPrice,
    string Status);

public sealed class UpdateVehicleEndpoint(ISender sender) : Endpoint<UpdateVehicleRequest, object>
{
    public override void Configure()
    {
        Put("/api/v1/vehicles/{id}");
        Roles("Manager");
        Description(x => x.WithTags("Vehicles"));
        Summary(s =>
        {
            s.Summary = "Update vehicle details";
            s.Description = "Updates the specification, pricing, mileage, and status of an existing vehicle.";
            s.Response(200, "Vehicle updated successfully.");
            s.Response(400, "Validation failed.");
            s.Response(404, "Vehicle not found.");
            s.Response(401, "Unauthorized.");
            s.Response(403, "Forbidden.");
        });
    }

    public override async Task HandleAsync(UpdateVehicleRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        if (!Enum.TryParse<FuelType>(req.FuelType, true, out var fuelType))
            fuelType = FuelType.Petrol;

        if (!Enum.TryParse<VehicleStatus>(req.Status, true, out var status))
            status = VehicleStatus.Available;

        var cmd = new UpdateVehicleCommand(
            id,
            req.Make,
            req.Model,
            req.Year,
            req.Color,
            req.Mileage,
            fuelType,
            req.PurchasePrice,
            req.AskingPrice,
            status);

        var result = await sender.Send(cmd, ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to update vehicle.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(new { data = new { id = result.Value } }, ct);
    }
}
