using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using VehicleStatusEnum = IID.Domain.Vehicles.VehicleStatus;

namespace IID.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository(IidDbContext db) : IVehicleRepository
{
    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Vehicles
            .Include(v => v.Dealership)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<bool> VinExistsAsync(string vin, CancellationToken ct)
    {
        var normalized = vin.Trim().ToUpperInvariant();
        try
        {
            var parsed = Vin.Parse(normalized);
            return await db.Vehicles.AsNoTracking().AnyAsync(v => v.Vin == parsed, ct);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public async Task<bool> StockNumberExistsAsync(string stockNumber, CancellationToken ct)
        => await db.Vehicles.AsNoTracking().AnyAsync(v => v.StockNumber == stockNumber.ToUpperInvariant(), ct);

    public async Task AddAsync(Vehicle vehicle, CancellationToken ct)
        => await db.Vehicles.AddAsync(vehicle, ct);

    public void Update(Vehicle vehicle) => db.Vehicles.Update(vehicle);
    public void Remove(Vehicle vehicle) => db.Vehicles.Remove(vehicle);

    public async Task<(IReadOnlyList<Vehicle> Items, int Total)> ListAsync(
        VehicleListFilter filter, CancellationToken ct = default)
    {
        IQueryable<Vehicle> q = db.Vehicles.AsNoTracking().Include(v => v.Dealership);
        if (filter.DealershipId.HasValue && filter.DealershipId.Value != Guid.Empty)
            q = q.Where(v => v.DealershipId == filter.DealershipId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Make)) q = q.Where(v => v.Make == filter.Make);
        if (!string.IsNullOrWhiteSpace(filter.Model)) q = q.Where(v => v.Model == filter.Model);
        if (!string.IsNullOrWhiteSpace(filter.Vin))
        {
            var vinTerm = filter.Vin.Trim();
            if (db.Database.ProviderName?.EndsWith("InMemory", StringComparison.OrdinalIgnoreCase) == true)
            {
                q = q.Where(v => v.Vin.Value.Contains(vinTerm));
            }
            else
            {
                q = q.Where(v => ((string)(object)v.Vin).Contains(vinTerm));
            }
        }
        if (!string.IsNullOrWhiteSpace(filter.StockNumber))
        {
            var stockTerm = filter.StockNumber.Trim();
            q = q.Where(v => v.StockNumber != null && v.StockNumber.Contains(stockTerm));
        }

        var effectiveMaxAge = filter.MaxAgeDays;
        if (!effectiveMaxAge.HasValue && filter.MinAgeDays.HasValue)
        {
            if (filter.MinAgeDays.Value == InventoryPolicy.AgingHighDays)
            {
                effectiveMaxAge = InventoryPolicy.AgingCriticalDays - 1;
            }
            else if (filter.MinAgeDays.Value == InventoryPolicy.AgingWarningDays)
            {
                effectiveMaxAge = InventoryPolicy.AgingHighDays - 1;
            }
        }

        if (filter.MinAgeDays.HasValue)
            q = q.Where(v => v.DateAddedToInventory <= DateTimeOffset.UtcNow.AddDays(-filter.MinAgeDays.Value));
        if (effectiveMaxAge.HasValue)
            q = q.Where(v => v.DateAddedToInventory >= DateTimeOffset.UtcNow.AddDays(-effectiveMaxAge.Value));

        if (filter.Status.HasValue) q = q.Where(v => v.Status == filter.Status.Value);

        q = (filter.Sort, filter.Order) switch
        {
            ("dateAdded", "asc") => q.OrderBy(v => v.DateAddedToInventory),
            ("dateAdded", _) => q.OrderByDescending(v => v.DateAddedToInventory),
            (_, "asc") => q.OrderBy(v => v.CreatedAt),
            _ => q.OrderByDescending(v => v.CreatedAt)
        };

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, filter.Page);
        var limit = Math.Clamp(filter.Limit, 1, 100);
        var items = await q.Skip((page - 1) * limit).Take(limit).ToListAsync(ct);
        return (items, total);
    }
}
