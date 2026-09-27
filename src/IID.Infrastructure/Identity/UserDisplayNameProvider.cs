using IID.Application.Common.Interfaces;
using IID.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;

namespace IID.Infrastructure.Identity;

public sealed class UserDisplayNameProvider(
    IidDbContext db,
    IMemoryCache cache) : IUserDisplayNameProvider
{
    private const string CacheKeyPrefix = "uid:display-name:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<string> GetDisplayNameAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return "Unknown";
        if (userId.Equals("seed", StringComparison.OrdinalIgnoreCase)) return "System";

        // If it's already an email or username
        if (userId.Contains('@')) return userId;

        var cacheKey = CacheKeyPrefix + userId;
        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
            return cached;

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email ?? u.UserName)
            .FirstOrDefaultAsync(ct);

        var resolved = string.IsNullOrWhiteSpace(user) ? "Admin" : user;
        cache.Set(cacheKey, resolved, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = 1
        });
        return resolved;
    }
}
