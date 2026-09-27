using IID.Application.Common.Interfaces;
using IID.Domain.Common;
using MediatR;

namespace IID.Application.Features.Dealerships.Queries.GetDealerships;

public sealed class GetDealershipsHandler(
    IDealershipRepository dealerships) : IRequestHandler<GetDealershipsQuery, Result<IReadOnlyList<DealershipDto>>>
{
    public async Task<Result<IReadOnlyList<DealershipDto>>> Handle(GetDealershipsQuery request, CancellationToken ct)
    {
        var items = await dealerships.ListAsync(ct);
        var counts = await dealerships.GetVehicleCountsAsync(ct);

        var dtos = items.Select(d => new DealershipDto(
            d.Id,
            d.Name,
            d.Code,
            d.City,
            d.State,
            d.Phone,
            counts.TryGetValue(d.Id, out var count) ? count : 0)).ToList();

        return Result<IReadOnlyList<DealershipDto>>.Success(dtos);
    }
}
