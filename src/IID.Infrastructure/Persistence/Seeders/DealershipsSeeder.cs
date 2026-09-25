using IID.Domain.Dealerships;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IID.Infrastructure.Persistence;

/// <summary>
/// Seeds 10 deterministic demo dealerships across key metropolitan regions.
/// Order is 15 so dealerships are created before VehiclesSeeder (Order 20).
/// </summary>
public sealed class DealershipsSeeder(IidDbContext db, TimeProvider clock, ILogger<DealershipsSeeder> logger) : IidSeeder
{
    public int Order => 15;

    public static readonly (Guid Id, string Name, string Code, string City, string State, string Phone)[] DealershipSeeds =
    [
        (Guid.Parse("11111111-1111-1111-1111-111111111101"), "Apex Motors - Downtown",        "DLR-LA-01",  "Los Angeles",   "CA", "(213) 555-0101"),
        (Guid.Parse("11111111-1111-1111-1111-111111111102"), "Metro Auto Group - North",       "DLR-SEA-02", "Seattle",       "WA", "(206) 555-0102"),
        (Guid.Parse("11111111-1111-1111-1111-111111111103"), "Summit Luxury Cars - Westside",  "DLR-DEN-03", "Denver",        "CO", "(303) 555-0103"),
        (Guid.Parse("11111111-1111-1111-1111-111111111104"), "Pinnacle Ford Lincoln",          "DLR-DAL-04", "Dallas",        "TX", "(214) 555-0104"),
        (Guid.Parse("11111111-1111-1111-1111-111111111105"), "Grand Horizon Toyota",          "DLR-PHX-05", "Phoenix",       "AZ", "(602) 555-0105"),
        (Guid.Parse("11111111-1111-1111-1111-111111111106"), "Velocity Performance Autos",     "DLR-MIA-06", "Miami",         "FL", "(305) 555-0106"),
        (Guid.Parse("11111111-1111-1111-1111-111111111107"), "Coastal Bay Auto Gallery",       "DLR-SFO-07", "San Francisco", "CA", "(415) 555-0107"),
        (Guid.Parse("11111111-1111-1111-1111-111111111108"), "Heritage Classic & Pre-Owned",   "DLR-CHI-08", "Chicago",       "IL", "(312) 555-0108"),
        (Guid.Parse("11111111-1111-1111-1111-111111111109"), "Frontier Auto Plaza",            "DLR-ATX-09", "Austin",        "TX", "(512) 555-0109"),
        (Guid.Parse("11111111-1111-1111-1111-111111111110"), "Silverstone Motor Cars",         "DLR-ATL-10", "Atlanta",       "GA", "(404) 555-0110")
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Dealerships.AnyAsync(ct))
        {
            logger.LogDebug("Dealerships already present ({Count} rows); skipping seeder.", await db.Dealerships.CountAsync(ct));
            return;
        }

        var now = clock.GetUtcNow();

        foreach (var s in DealershipSeeds)
        {
            var dealership = Dealership.Create(s.Name, s.Code, s.City, s.State, s.Phone, now, s.Id);
            await db.Dealerships.AddAsync(dealership, ct);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} dealerships successfully.", DealershipSeeds.Length);
    }
}
