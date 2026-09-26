using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IID.Application.Common.Interfaces;
using IID.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace IID.Infrastructure.Persistence.Repositories;

public sealed class UserActivityReadRepository(IidDbContext db) : IUserActivityReadRepository
{
    public async Task<IReadOnlySet<string>> GetReadActivityIdsAsync(string userId, CancellationToken ct)
    {
        var ids = await db.UserActivityReadStatuses
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsRead)
            .Select(x => x.ActivityId)
            .ToListAsync(ct);

        return ids.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct)
    {
        var readIds = await db.UserActivityReadStatuses
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsRead)
            .Select(x => x.ActivityId)
            .ToListAsync(ct);

        if (readIds.Count == 0)
        {
            return await db.VehicleActions.AsNoTracking().CountAsync(ct);
        }

        var readGuids = readIds
            .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        if (readGuids.Count < 2000)
        {
            return await db.VehicleActions
                .AsNoTracking()
                .CountAsync(a => !readGuids.Contains(a.Id), ct);
        }

        var total = await db.VehicleActions.AsNoTracking().CountAsync(ct);
        return Math.Max(0, total - readGuids.Count);
    }

    public async Task MarkAsReadAsync(string userId, IEnumerable<string> activityIds, CancellationToken ct)
    {
        var targetIds = activityIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (targetIds.Count == 0) return;

        var existing = await db.UserActivityReadStatuses
            .Where(x => x.UserId == userId && targetIds.Contains(x.ActivityId))
            .ToListAsync(ct);

        var existingSet = existing.Select(x => x.ActivityId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var ex in existing)
        {
            if (!ex.IsRead)
            {
                ex.MarkAsRead(DateTimeOffset.UtcNow);
            }
        }

        foreach (var id in targetIds)
        {
            if (!existingSet.Contains(id))
            {
                db.UserActivityReadStatuses.Add(UserActivityReadStatus.Create(userId, id));
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken ct)
    {
        var existing = await db.UserActivityReadStatuses
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

        var existingSet = existing.Select(x => x.ActivityId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var ex in existing)
        {
            if (!ex.IsRead)
            {
                ex.MarkAsRead(DateTimeOffset.UtcNow);
            }
        }

        var allActionGuids = await db.VehicleActions
            .AsNoTracking()
            .Select(a => a.Id)
            .ToListAsync(ct);

        var toAdd = allActionGuids
            .Where(g => !existingSet.Contains(g.ToString()))
            .Select(g => UserActivityReadStatus.Create(userId, g.ToString()))
            .ToList();

        if (toAdd.Count > 0)
        {
            await db.UserActivityReadStatuses.AddRangeAsync(toAdd, ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
