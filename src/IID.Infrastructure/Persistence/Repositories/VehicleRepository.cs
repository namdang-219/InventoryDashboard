using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using VehicleStatusEnum = IID.Domain.Vehicles.VehicleStatus;

namespace IID.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository(IidDbContext db) : IVehicleRepository
{
    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<Vehicle>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];
        return await db.Vehicles.AsNoTracking().Where(v => idList.Contains(v.Id)).ToListAsync(ct);
    }

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

    public void SetRowVersion(Vehicle vehicle, byte[] rowVersion)
    {
        vehicle.SetRowVersion(rowVersion);
        db.Entry(vehicle).Property(x => x.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<(IReadOnlyList<Vehicle> Items, int Total)> ListAsync(
        VehicleListFilter filter, CancellationToken ct = default)
    {
        IQueryable<Vehicle> q = db.Vehicles.AsNoTracking();
        if (filter.DealershipId.HasValue && filter.DealershipId.Value != Guid.Empty)
            q = q.Where(v => v.DealershipId == filter.DealershipId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Make)) q = q.Where(v => v.Make == filter.Make);
        if (!string.IsNullOrWhiteSpace(filter.Model)) q = q.Where(v => v.Model == filter.Model);
        if (!string.IsNullOrWhiteSpace(filter.Vin))
        {
            var vinTerm = filter.Vin.Trim();
            q = q.Where(v => v.Vin.Value.Contains(vinTerm));
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

        if (filter.Status.HasValue)
        {
            q = q.Where(v => v.Status == filter.Status.Value);
        }
        else if (filter.ExcludeSold)
        {
            q = q.Where(v => v.Status != VehicleStatus.Sold);
        }

        q = (filter.Sort?.ToLowerInvariant(), filter.Order?.ToLowerInvariant()) switch
        {
            ("dateadded", "asc") => q.OrderBy(v => v.DateAddedToInventory).ThenBy(v => v.Id),
            ("dateadded", _) => q.OrderByDescending(v => v.DateAddedToInventory).ThenByDescending(v => v.Id),
            ("soldat" or "solddate" or "datesold", "asc") => q.OrderBy(v => v.SoldAt == null ? 1 : 0).ThenBy(v => v.SoldAt).ThenBy(v => v.Id),
            ("soldat" or "solddate" or "datesold", _) => q.OrderBy(v => v.SoldAt == null ? 1 : 0).ThenByDescending(v => v.SoldAt).ThenByDescending(v => v.Id),
            ("askingprice", "asc") => q.OrderBy(v => v.AskingPrice.Amount).ThenBy(v => v.Id),
            ("askingprice", _) => q.OrderByDescending(v => v.AskingPrice.Amount).ThenByDescending(v => v.Id),
            ("mileage", "asc") => q.OrderBy(v => v.Mileage).ThenBy(v => v.Id),
            ("mileage", _) => q.OrderByDescending(v => v.Mileage).ThenByDescending(v => v.Id),
            ("year", "asc") => q.OrderBy(v => v.Year).ThenBy(v => v.Id),
            ("year", _) => q.OrderByDescending(v => v.Year).ThenByDescending(v => v.Id),
            (_, "asc") => q.OrderBy(v => v.CreatedAt).ThenBy(v => v.Id),
            _ => q.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id)
        };

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, filter.Page);
        var limit = Math.Clamp(filter.Limit, 1, 1000);
        var items = await q.Skip((page - 1) * limit).Take(limit).ToListAsync(ct);
        return (items, total);
    }
}
