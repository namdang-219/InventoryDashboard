using MediatR;

namespace IID.Application.VehicleActions.Commands.LogVehicleAction;

public sealed record LogVehicleActionCommand(
    Guid VehicleId,
    VehicleActionType ActionType,
    string? Notes) : IRequest<Result<Guid>>;
