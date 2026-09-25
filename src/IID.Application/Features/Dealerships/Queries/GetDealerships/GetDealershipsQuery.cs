using IID.Domain.Common;
using MediatR;

namespace IID.Application.Features.Dealerships.Queries.GetDealerships;

public sealed record GetDealershipsQuery : IRequest<Result<IReadOnlyList<DealershipDto>>>;
