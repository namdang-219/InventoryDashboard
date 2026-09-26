using IID.Application.Common.Interfaces;
using IID.Domain.Dealerships;

namespace IID.Infrastructure.Persistence.Repositories;

public sealed class DealershipRepository(IidDbContext db) : IDealershipRepository
{
    public async Task<Dealership?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Dealerships
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Dealership>> ListAsync(CancellationToken ct)
        => await db.Dealerships
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, int>> GetVehicleCountsAsync(CancellationToken ct)
    {
        return await db.Vehicles
            .AsNoTracking()
            .GroupBy(v => v.DealershipId)
            .Select(g => new { DealershipId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DealershipId, x => x.Count, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken ct)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return await db.Dealerships
            .AnyAsync(d => d.Code == normalized && (!excludeId.HasValue || d.Id != excludeId.Value), ct);
    }

    public async Task AddAsync(Dealership dealership, CancellationToken ct)
        => await db.Dealerships.AddAsync(dealership, ct);

    public void Update(Dealership dealership)
        => db.Dealerships.Update(dealership);
}
