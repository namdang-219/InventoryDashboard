using IID.Domain.Common;
using MediatR;

namespace IID.Application.Features.Dealerships.Commands.UpdateDealership;

public sealed record UpdateDealershipCommand(
    Guid Id,
    string Name,
    string Code,
    string City,
    string State,
    string Phone) : IRequest<Result<Guid>>;
