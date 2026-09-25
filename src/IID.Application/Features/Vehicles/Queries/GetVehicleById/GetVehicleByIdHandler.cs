using IID.Application.Common.Interfaces;
using IID.Application.Vehicles.Queries.Dtos;
using MediatR;

namespace IID.Application.Vehicles.Queries.GetVehicleById;

public sealed class GetVehicleByIdHandler(
    IVehicleRepository vehicles,
    IClock clock) : IRequestHandler<GetVehicleByIdQuery, Result<VehicleResponse>>
{
    public async Task<Result<VehicleResponse>> Handle(GetVehicleByIdQuery q, CancellationToken ct)
    {
        var vehicle = await vehicles.GetByIdAsync(q.Id, ct);
        if (vehicle is null)
            return Result<VehicleResponse>.Failure(ErrorKind.NotFound, "Vehicle not found.");

        var analytics = new VehicleAnalyticsService(clock.UtcNow);
        return Result<VehicleResponse>.Success(VehicleResponse.From(vehicle, analytics));
    }
}
