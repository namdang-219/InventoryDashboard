using IID.Domain.Common;
using MediatR;

namespace IID.Application.Vehicles.Commands.TransferDealership;

public sealed record TransferDealershipCommand(
    Guid VehicleId,
    Guid TargetDealershipId,
    string? Notes = null) : IRequest<Result<Guid>>;
