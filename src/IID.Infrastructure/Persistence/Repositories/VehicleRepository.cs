using System;
using System.Globalization;
using System.Text;
using IID.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
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
        string? make, string? model, int? minAgeDays, int? maxAgeDays, VehicleStatusEnum? status,
        int page, int limit, string sort, string order, CancellationToken ct, Guid? dealershipId = null)
    {
        IQueryable<Vehicle> q = db.Vehicles.AsNoTracking().Include(v => v.Dealership);
        if (dealershipId.HasValue && dealershipId.Value != Guid.Empty)
            q = q.Where(v => v.DealershipId == dealershipId.Value);

        if (!string.IsNullOrWhiteSpace(make)) q = q.Where(v => v.Make == make);
        if (!string.IsNullOrWhiteSpace(model)) q = q.Where(v => v.Model == model);

        // Age filters must be expressed in SQL-translatable form. We compute the
        // cutoff date in C# and pass it as a parameter so EF emits
        // `WHERE DateAddedToInventory <= @cutoff`, which SQL Server can execute
        // directly. The previous form `(UtcNow - v.DateAddedToInventory).TotalDays`
        // cannot be translated by EF 10 and throws at runtime.
        if (minAgeDays.HasValue)
            q = q.Where(v => v.DateAddedToInventory <= DateTimeOffset.UtcNow.AddDays(-minAgeDays.Value));
        if (maxAgeDays.HasValue)
            q = q.Where(v => v.DateAddedToInventory >= DateTimeOffset.UtcNow.AddDays(-maxAgeDays.Value));

        if (status.HasValue) q = q.Where(v => v.Status == status.Value);

        q = (sort, order) switch
        {
            ("dateAdded", "asc") => q.OrderBy(v => v.DateAddedToInventory),
            ("dateAdded", _) => q.OrderByDescending(v => v.DateAddedToInventory),
            (_, "asc") => q.OrderBy(v => v.CreatedAt),
            _ => q.OrderByDescending(v => v.CreatedAt)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * limit).Take(limit).ToListAsync(ct);
        return (items, total);
    }
}
