using System.Globalization;
using System.Text;
using IID.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

using IID.Application.Common.Models;

namespace IID.Infrastructure.Persistence.Repositories;

public sealed class VehicleActionRepository(IidDbContext db) : IVehicleActionRepository
{
    public async Task AddAsync(Domain.VehicleActions.VehicleAction action, CancellationToken ct)
        => await db.VehicleActions.AddAsync(action, ct);

    public void Remove(Domain.VehicleActions.VehicleAction action) => db.VehicleActions.Remove(action);

    public async Task<(IReadOnlyList<Domain.VehicleActions.VehicleAction> Items, int Total)> ListAsync(
        VehicleActionListFilter filter, CancellationToken ct = default)
    {
        IQueryable<Domain.VehicleActions.VehicleAction> q = db.VehicleActions.AsNoTracking();
        if (filter.VehicleId.HasValue) q = q.Where(a => a.VehicleId == filter.VehicleId.Value);
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, filter.Page);
        var limit = Math.Clamp(filter.Limit, 1, 100);
        var items = await q.OrderByDescending(a => a.LoggedAt).Skip((page - 1) * limit).Take(limit).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Domain.VehicleActions.VehicleAction> Items, string? NextCursor, bool HasMore, int Total)> ListByCursorAsync(
        string? cursor, int limit, CancellationToken ct)
    {
        if (limit <= 0) limit = 10;
        if (limit > 50) limit = 50;

        IQueryable<Domain.VehicleActions.VehicleAction> q = db.VehicleActions.AsNoTracking();
        var total = await q.CountAsync(ct);

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                var parts = decoded.Split('|');
                if (parts.Length == 2 &&
                    DateTimeOffset.TryParse(parts[0], null, DateTimeStyles.RoundtripKind, out var cursorTime) &&
                    Guid.TryParse(parts[1], out var cursorId))
                {
                    q = q.Where(a => a.LoggedAt < cursorTime || (a.LoggedAt == cursorTime && a.Id != cursorId));
                }
            }
            catch
            {
                // Fallback to beginning if cursor is invalid
            }
        }

        q = q.OrderByDescending(a => a.LoggedAt).ThenByDescending(a => a.Id);

        var actions = await q.Take(limit + 1).ToListAsync(ct);
        var hasMore = actions.Count > limit;
        var items = hasMore ? actions.Take(limit).ToList() : actions;

        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var last = items[^1];
            var rawCursor = $"{last.LoggedAt:O}|{last.Id}";
            nextCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCursor));
        }

        return (items, nextCursor, hasMore, total);
    }
}
