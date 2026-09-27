using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using IID.Domain.Common;
using IID.Domain.Dealerships;
using MediatR;

namespace IID.Application.Features.Dealerships.Commands.CreateDealership;

public sealed class CreateDealershipHandler(
    IDealershipRepository repository,
    IUnitOfWork uow,
    IClock clock,
    ILogger<CreateDealershipHandler> logger) : IRequestHandler<CreateDealershipCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDealershipCommand req, CancellationToken ct)
    {
        var dealership = Dealership.Create(
            req.Name,
            req.Code,
            req.City,
            req.State,
            req.Phone,
            clock.UtcNow);

        await repository.AddAsync(dealership, ct);
        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
        {
            logger.CreateDealershipFailed(req.Code, saveResult.Message ?? "Failed to save dealership.", null);
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Failed to save dealership.");
        }

        logger.DealershipCreated(dealership.Id, dealership.Code, dealership.Name);
        return Result<Guid>.Success(dealership.Id);
    }
}
