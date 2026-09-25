using IID.Application.Common.Interfaces;
using IID.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IID.Application.Features.Dealerships.Queries.GetDealerships;

public sealed class GetDealershipsHandler(
    IDealershipRepository dealerships,
    ILogger<GetDealershipsHandler> logger) : IRequestHandler<GetDealershipsQuery, Result<IReadOnlyList<DealershipDto>>>
{
    public async Task<Result<IReadOnlyList<DealershipDto>>> Handle(GetDealershipsQuery request, CancellationToken ct)
    {
        var items = await dealerships.ListAsync(ct);
        var dtos = items.Select(DealershipDto.From).ToList();

        logger.LogDebug("Retrieved {Count} dealerships", dtos.Count);
        return Result<IReadOnlyList<DealershipDto>>.Success(dtos);
    }
}
