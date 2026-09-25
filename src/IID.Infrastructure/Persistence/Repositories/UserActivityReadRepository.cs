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

        var readIdSet = readIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allActionGuids = await db.VehicleActions
            .AsNoTracking()
            .Select(a => a.Id)
            .ToListAsync(ct);

        return allActionGuids.Count(guid => !readIdSet.Contains(guid.ToString()));
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
                ex.IsRead = true;
                ex.ReadAtUtc = DateTimeOffset.UtcNow;
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
        var allActionGuids = await db.VehicleActions
            .AsNoTracking()
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (allActionGuids.Count == 0) return;

        var existing = await db.UserActivityReadStatuses
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

        var existingSet = existing.Select(x => x.ActivityId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var ex in existing)
        {
            if (!ex.IsRead)
            {
                ex.IsRead = true;
                ex.ReadAtUtc = DateTimeOffset.UtcNow;
            }
        }

        var newStatuses = new List<UserActivityReadStatus>();
        foreach (var actGuid in allActionGuids)
        {
            var actId = actGuid.ToString();
            if (!existingSet.Contains(actId))
            {
                newStatuses.Add(UserActivityReadStatus.Create(userId, actId));
                existingSet.Add(actId);
            }
        }

        if (newStatuses.Count > 0)
        {
            await db.UserActivityReadStatuses.AddRangeAsync(newStatuses, ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
