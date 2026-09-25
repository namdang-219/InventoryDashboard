using IID.Application.Common.Models;
using IID.Domain.VehicleActions;
using MediatR;

namespace IID.Application.VehicleActions.Commands.UpdateVehicleAction;

public sealed record UpdateVehicleActionCommand(
    Guid Id,
    VehicleActionType ActionType,
    string? Notes) : IRequest<Result<Unit>>;
