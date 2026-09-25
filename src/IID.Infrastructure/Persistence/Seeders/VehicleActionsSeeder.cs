using Microsoft.Extensions.Logging;

namespace IID.Infrastructure.Persistence;

/// <summary>
/// Seeds a small set of <see cref="VehicleAction"/> rows distributed across the
/// demo vehicles so the action log has content on first boot.
///
/// Idempotent: only runs when <c>VehicleActions</c> is empty.
/// Relies on <see cref="VehiclesSeeder"/> having run first (Order = 30 vs 20).
/// </summary>
public sealed class VehicleActionsSeeder(IidDbContext db, TimeProvider clock, ILogger<VehicleActionsSeeder> logger) : IidSeeder
{
    public int Order => 30;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.VehicleActions.AnyAsync(ct))
        {
            logger.LogDebug("VehicleActions already present ({Count} rows); skipping seeder.", await db.VehicleActions.CountAsync(ct));
            return;
        }

        var vehicles = await db.Vehicles.AsNoTracking().ToListAsync(ct);
        if (vehicles.Count == 0)
        {
            logger.LogDebug("No vehicles to attach actions to; skipping.");
            return;
        }

        var now = clock.GetUtcNow();

        // (vehicleIndex, actionType, daysAgo, notes)
        var seeds = new (int VIdx, VehicleActionType Type, int DaysAgo, string? Notes)[]
        {
            // Fresh vehicles (1–6)
            (0, VehicleActionType.PriceReductionPlanned,  1,  "Review pricing competitiveness"),
            (0, VehicleActionType.ManagerReview,         2,  null),
            (1, VehicleActionType.TradeInCustomer,        3,  "Customer inquiry follow-up"),
            (2, VehicleActionType.Other,                  5,  "Detail and photos updated"),
            (3, VehicleActionType.PriceReductionPlanned,  2,  "Reduce $800"),
            (4, VehicleActionType.ManagerReview,          7,  "Floor plan review"),
            (5, VehicleActionType.Relist,                 10,  "Refresh listing photos"),

            // Mid-age vehicles (6–9)
            (6, VehicleActionType.DealerAuction,           8,  "Listed on Manheim"),
            (7, VehicleActionType.PriceReductionPlanned,  3,  "Reduce $1,200"),
            (7, VehicleActionType.Other,                  1,  "Interior deep clean"),
            (8, VehicleActionType.ManagerReview,         12,  "Confirm pricing strategy"),
            (9, VehicleActionType.TradeInCustomer,        4,  "Appraisal request"),

            // Near-threshold vehicles (10–14)
            (10, VehicleActionType.PriceReductionPlanned, 5,  "Reduce $1,500"),
            (10, VehicleActionType.Other,                14,  "Tire rotation completed"),
            (11, VehicleActionType.ManagerReview,        8,  "Market analysis review"),
            (12, VehicleActionType.DealerAuction,        3,  "Sent to auction"),
            (13, VehicleActionType.TradeInCustomer,      6,  "Customer negotiation"),
            (14, VehicleActionType.Relist,               20,  "Re-listed with new photos"),

            // Aging vehicles (15–23)
            (15, VehicleActionType.PriceReductionPlanned, 2,  "Reduce $2,000"),
            (15, VehicleActionType.ManagerReview,        15,  "Aging inventory review"),
            (16, VehicleActionType.DealerAuction,        10,  "Moved to auction lane"),
            (17, VehicleActionType.Other,                 5,  "Wholesale listing updated"),
            (18, VehicleActionType.TradeInCustomer,      7,  "Trade-in appraisal"),
            (19, VehicleActionType.ManagerReview,        3,  "Price adjustment approved"),
            (20, VehicleActionType.PriceReductionPlanned, 1, "Reduce $1,800"),
            (21, VehicleActionType.Relist,               25,  "Fresh photos uploaded"),
            (22, VehicleActionType.Other,                12,  "Mechanical inspection done"),

            // Long-term vehicles (24–29)
            (24, VehicleActionType.PriceReductionPlanned, 3,  "Reduce $3,000"),
            (24, VehicleActionType.ManagerReview,        30,  "Final pricing review"),
            (25, VehicleActionType.DealerAuction,         8,  "Sent to wholesale auction"),
            (26, VehicleActionType.Other,                 6,  "Smoke smell remediation"),
            (27, VehicleActionType.TradeInCustomer,      14,  "Customer buy-back offer"),
            (28, VehicleActionType.PriceReductionPlanned, 2, "Significant price cut"),
            (29, VehicleActionType.ManagerReview,        45,  "End-of-life inventory decision"),
        };

        var vehicleIds = vehicles.Select(v => v.Id).ToArray();

        foreach (var s in seeds)
        {
            if (s.VIdx < 0 || s.VIdx >= vehicleIds.Length) continue;
            var action = VehicleAction.Log(
                vehicleId: vehicleIds[s.VIdx],
                actionType: s.Type,
                notes: s.Notes,
                loggedByUserId: "seed",
                nowUtc: now.AddDays(-s.DaysAgo));
            db.VehicleActions.Add(action);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} demo vehicle actions.", seeds.Length);
    }
}
