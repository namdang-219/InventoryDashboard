using IID.Application.Common.Interfaces;
using IID.Domain.Common;
using IID.Domain.Dealerships;
using MediatR;
using Microsoft.Extensions.Logging;

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
            logger.LogError("Failed to persist dealership {Code}: {Message}", req.Code, saveResult.Message);
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Failed to save dealership.");
        }

        logger.LogInformation("Created dealership {Id} ({Code} - {Name})", dealership.Id, dealership.Code, dealership.Name);
        return Result<Guid>.Success(dealership.Id);
    }
}
