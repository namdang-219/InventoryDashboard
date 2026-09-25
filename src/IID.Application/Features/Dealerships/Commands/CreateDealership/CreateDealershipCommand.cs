using IID.Domain.Common;
using MediatR;

namespace IID.Application.Features.Dealerships.Commands.CreateDealership;

public sealed record CreateDealershipCommand(
    string Name,
    string Code,
    string City,
    string State,
    string Phone) : IRequest<Result<Guid>>;
