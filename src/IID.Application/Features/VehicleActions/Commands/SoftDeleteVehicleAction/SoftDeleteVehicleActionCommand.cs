using IID.Application.Common.Models;
using MediatR;

namespace IID.Application.VehicleActions.Commands.SoftDeleteVehicleAction;

public sealed record SoftDeleteVehicleActionCommand(Guid Id) : IRequest<Result<Unit>>;
