using IID.Application.Common.Interfaces;
using IID.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IID.Application.Features.Dealerships.Commands.UpdateDealership;

public sealed class UpdateDealershipHandler(
    IDealershipRepository repository,
    IUnitOfWork uow,
    IClock clock,
    ILogger<UpdateDealershipHandler> logger) : IRequestHandler<UpdateDealershipCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateDealershipCommand req, CancellationToken ct)
    {
        var dealership = await repository.GetByIdAsync(req.Id, ct);
        if (dealership is null)
        {
            return Result<Guid>.Failure(ErrorKind.NotFound, $"Dealership {req.Id} was not found.");
        }

        dealership.Update(
            req.Name,
            req.Code,
            req.City,
            req.State,
            req.Phone,
            clock.UtcNow);

        repository.Update(dealership);
        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
        {
            logger.LogError("Failed to update dealership {Id}: {Message}", req.Id, saveResult.Message);
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Failed to update dealership.");
        }

        logger.LogInformation("Updated dealership {Id} ({Code} - {Name})", dealership.Id, dealership.Code, dealership.Name);
        return Result<Guid>.Success(dealership.Id);
    }
}
