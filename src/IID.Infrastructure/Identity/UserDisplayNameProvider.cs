using IID.Application.Common.Interfaces;
using IID.Infrastructure.Persistence;
namespace IID.Infrastructure.Identity;

public sealed class UserDisplayNameProvider(IidDbContext db) : IUserDisplayNameProvider
{
    private static readonly Dictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Lock = new();

    public async Task<string> GetDisplayNameAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return "Unknown";
        if (userId.Equals("seed", StringComparison.OrdinalIgnoreCase)) return "System";

        // If it's already an email or username
        if (userId.Contains('@')) return userId;

        lock (Lock)
        {
            if (Cache.TryGetValue(userId, out var cached))
                return cached;
        }

        try
        {
            var user = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Email ?? u.UserName)
                .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrWhiteSpace(user))
            {
                lock (Lock) { Cache[userId] = user; }
                return user;
            }
        }
        catch
        {
            // fallback if db error
        }

        return "Admin";
    }
}
