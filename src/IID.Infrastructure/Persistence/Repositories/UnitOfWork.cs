using IID.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IID.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(IidDbContext db) : IUnitOfWork
{
    public async Task<Result<int>> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            var rows = await db.SaveChangesAsync(ct);
            return Result<int>.Success(rows);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Optimistic concurrency: another writer modified/deleted the row
            // before this UPDATE could land. Surface as Conflict so the API
            // responds 409 without exposing EF Core types to the caller.
            return Result<int>.Failure(
                ErrorKind.Conflict,
                "The record was modified by another user. Please reload and retry.");
        }
    }
}
