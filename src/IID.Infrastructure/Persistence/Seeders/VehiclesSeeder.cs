using IID.Domain.Dealerships;
using IID.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IID.Infrastructure.Persistence;

/// <summary>
/// Seeds realistic automotive inventory across all 10 dealerships.
/// Ensures each dealership has between 50 and 100 vehicles (average ~70-75 units per dealership),
/// providing comprehensive coverage for fresh inventory, aging alerts, pending deals, wholesale,
/// and various fuel types/makes.
/// </summary>
public sealed class VehiclesSeeder(IidDbContext db, TimeProvider clock, ILogger<VehiclesSeeder> logger) : IidSeeder
{
    public int Order => 20;

    private static readonly int[] TargetCountsPerDealership = [75, 68, 82, 56, 92, 64, 78, 86, 60, 72];

    private static readonly string[] Colors =
    [
        "Pearl White", "Jet Black", "Silver Metallic", "Charcoal Gray",
        "Deep Blue", "Ruby Red", "Emerald Green", "Graphite Gray",
        "Alpine White", "Midnight Silver"
    ];

    private static readonly (string Make, string Model, FuelType FuelType, decimal BasePurchase, decimal BaseAsking)[] VehicleTemplates =
    [
        // Toyota
        ("Toyota", "Camry", FuelType.Hybrid, 24_000m, 27_500m),
        ("Toyota", "Corolla", FuelType.Petrol, 19_500m, 22_800m),
        ("Toyota", "RAV4", FuelType.Hybrid, 28_000m, 32_200m),
        ("Toyota", "Highlander", FuelType.Hybrid, 38_500m, 43_900m),
        ("Toyota", "Tacoma", FuelType.Petrol, 32_000m, 36_500m),
        ("Toyota", "Prius", FuelType.Hybrid, 26_000m, 29_900m),
        ("Toyota", "Tundra", FuelType.Petrol, 44_000m, 49_800m),

        // Honda
        ("Honda", "Civic", FuelType.Petrol, 21_000m, 24_200m),
        ("Honda", "Accord", FuelType.Hybrid, 26_500m, 30_500m),
        ("Honda", "CR-V", FuelType.Hybrid, 29_000m, 33_400m),
        ("Honda", "Pilot", FuelType.Petrol, 37_000m, 42_000m),
        ("Honda", "HR-V", FuelType.Petrol, 23_000m, 26_500m),

        // Ford
        ("Ford", "F-150 XLT", FuelType.Petrol, 42_000m, 47_500m),
        ("Ford", "Explorer", FuelType.Petrol, 35_000m, 39_900m),
        ("Ford", "Mustang GT", FuelType.Petrol, 38_000m, 43_500m),
        ("Ford", "Bronco", FuelType.Petrol, 39_000m, 44_900m),
        ("Ford", "Escape", FuelType.Hybrid, 27_000m, 31_000m),
        ("Ford", "Mustang Mach-E", FuelType.Electric, 41_000m, 46_800m),

        // Tesla
        ("Tesla", "Model 3", FuelType.Electric, 36_000m, 41_000m),
        ("Tesla", "Model Y", FuelType.Electric, 40_000m, 45_500m),
        ("Tesla", "Model S", FuelType.Electric, 68_000m, 76_000m),
        ("Tesla", "Model X", FuelType.Electric, 74_000m, 82_500m),

        // BMW
        ("BMW", "3 Series", FuelType.Petrol, 34_000m, 38_900m),
        ("BMW", "5 Series", FuelType.Petrol, 46_000m, 52_500m),
        ("BMW", "X3", FuelType.Petrol, 39_000m, 44_200m),
        ("BMW", "X5", FuelType.PluginHybrid, 53_000m, 59_900m),
        ("BMW", "M4 Competition", FuelType.Petrol, 69_000m, 78_000m),
        ("BMW", "i4", FuelType.Electric, 45_000m, 51_000m),

        // Mercedes
        ("Mercedes", "C-Class", FuelType.Petrol, 35_000m, 39_800m),
        ("Mercedes", "E-Class", FuelType.Petrol, 48_000m, 54_900m),
        ("Mercedes", "GLC 300", FuelType.Petrol, 42_000m, 47_800m),
        ("Mercedes", "GLE 350", FuelType.Petrol, 52_000m, 58_900m),
        ("Mercedes", "EQE", FuelType.Electric, 59_000m, 66_500m),

        // Audi
        ("Audi", "A4", FuelType.Petrol, 33_000m, 37_500m),
        ("Audi", "A6", FuelType.Petrol, 45_000m, 51_200m),
        ("Audi", "Q5", FuelType.Petrol, 38_000m, 43_500m),
        ("Audi", "Q7", FuelType.Petrol, 51_000m, 57_900m),
        ("Audi", "e-tron GT", FuelType.Electric, 78_000m, 87_000m),

        // Chevrolet & GMC
        ("Chevrolet", "Silverado 1500", FuelType.Petrol, 43_000m, 48_500m),
        ("Chevrolet", "Equinox", FuelType.Petrol, 23_000m, 26_800m),
        ("Chevrolet", "Tahoe", FuelType.Petrol, 54_000m, 61_000m),
        ("Chevrolet", "Corvette Stingray", FuelType.Petrol, 65_000m, 74_000m),
        ("GMC", "Sierra 1500", FuelType.Petrol, 45_000m, 50_900m),
        ("GMC", "Yukon", FuelType.Petrol, 58_000m, 65_500m),

        // Hyundai & Kia
        ("Hyundai", "Elantra", FuelType.Petrol, 18_500m, 21_500m),
        ("Hyundai", "Tucson", FuelType.Hybrid, 27_000m, 31_000m),
        ("Hyundai", "Palisade", FuelType.Petrol, 38_000m, 43_200m),
        ("Hyundai", "Ioniq 5", FuelType.Electric, 37_000m, 42_000m),
        ("Kia", "Sportage", FuelType.Hybrid, 26_500m, 30_400m),
        ("Kia", "Telluride", FuelType.Petrol, 39_000m, 44_500m),
        ("Kia", "EV6", FuelType.Electric, 38_500m, 43_800m),

        // Subaru & Mazda
        ("Subaru", "Outback", FuelType.Petrol, 28_000m, 32_000m),
        ("Subaru", "Forester", FuelType.Petrol, 27_500m, 31_500m),
        ("Subaru", "Crosstrek", FuelType.Petrol, 23_500m, 26_900m),
        ("Mazda", "CX-5", FuelType.Petrol, 26_000m, 29_800m),
        ("Mazda", "CX-30", FuelType.Petrol, 22_500m, 25_800m),
        ("Mazda", "CX-90", FuelType.PluginHybrid, 42_000m, 47_900m),

        // Lexus & Porsche & Jeep
        ("Lexus", "RX 350", FuelType.Hybrid, 46_000m, 52_000m),
        ("Lexus", "ES 350", FuelType.Petrol, 39_000m, 44_200m),
        ("Lexus", "NX 350", FuelType.Hybrid, 37_500m, 42_800m),
        ("Porsche", "Macan", FuelType.Petrol, 56_000m, 63_500m),
        ("Porsche", "Cayenne", FuelType.Petrol, 72_000m, 81_000m),
        ("Porsche", "Taycan", FuelType.Electric, 82_000m, 92_500m),
        ("Jeep", "Wrangler", FuelType.Petrol, 36_000m, 41_200m),
        ("Jeep", "Grand Cherokee", FuelType.Petrol, 40_000m, 45_800m)
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existingCount = await db.Vehicles.CountAsync(ct);
        if (existingCount >= 500)
        {
            logger.LogDebug("Vehicles already seeded ({Count} rows); skipping seeder.", existingCount);
            return;
        }

        var now = clock.GetUtcNow();
        var dealerships = await db.Dealerships.OrderBy(d => d.Id).ToListAsync(ct);
        if (dealerships.Count == 0)
        {
            logger.LogWarning("No dealerships found in database. Seed Dealerships first.");
            return;
        }

        var vehicleCountByDealer = await db.Vehicles
            .GroupBy(v => v.DealershipId)
            .Select(g => new { DealerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DealerId, x => x.Count, ct);

        var existingVins = (await db.Vehicles.Select(v => v.Vin).ToListAsync(ct))
            .Select(v => v.Value)
            .ToHashSet();

        var vehiclesToAdd = new List<Vehicle>();
        var vinCounter = 1000 + existingCount;

        for (var i = 0; i < dealerships.Count; i++)
        {
            var dealer = dealerships[i];
            var current = vehicleCountByDealer.GetValueOrDefault(dealer.Id, 0);
            var target = TargetCountsPerDealership[i % TargetCountsPerDealership.Length];
            var needed = target - current;

            if (needed <= 0) continue;

            for (var j = 0; j < needed; j++)
            {
                var template = VehicleTemplates[(i * 13 + j * 7) % VehicleTemplates.Length];
                var color = Colors[(i + j * 3) % Colors.Length];

                // Aging distribution
                int daysAgo;
                VehicleStatus status;
                var bucket = j % 10;

                if (bucket < 4) // 40% Fresh (3-28 days)
                {
                    daysAgo = 3 + (j * 7) % 25;
                    status = (j % 5 == 0) ? VehicleStatus.Pending : VehicleStatus.Available;
                }
                else if (bucket < 6) // 20% Mid-age (31-58 days)
                {
                    daysAgo = 31 + (j * 5) % 27;
                    status = (j % 4 == 0) ? VehicleStatus.Pending : VehicleStatus.Available;
                }
                else if (bucket < 8) // 20% Near-threshold (61-89 days)
                {
                    daysAgo = 61 + (j * 3) % 28;
                    status = (j % 6 == 0) ? VehicleStatus.Pending : VehicleStatus.Available;
                }
                else if (bucket == 8) // 10% Aging (91-175 days)
                {
                    daysAgo = 91 + (j * 7) % 84;
                    status = (j % 3 == 0) ? VehicleStatus.Wholesale : VehicleStatus.Available;
                }
                else // 10% Long-term (181-270 days)
                {
                    daysAgo = 181 + (j * 9) % 89;
                    status = (j % 2 == 0) ? VehicleStatus.Wholesale : VehicleStatus.Available;
                }

                // Year correlated with days ago
                var year = daysAgo switch
                {
                    < 40 => 2024 + (j % 2),
                    < 90 => 2023,
                    < 160 => 2022,
                    < 220 => 2021,
                    _ => 2019 + (j % 2)
                };

                var mileage = (2025 - year) * 11_200 + daysAgo * 25 + (j % 1500);
                var purchase = template.BasePurchase + (j % 5) * 500m;
                var asking = template.BaseAsking + (j % 5) * 650m;

                var vinStr = GenerateUniqueVin(vinCounter++, template.Make, existingVins);
                var stockNumber = $"STK-{dealer.Code.Replace("DLR-", "")}-{j + 101:D3}";

                var vehicle = Vehicle.Create(
                    vin: Vin.Parse(vinStr),
                    make: template.Make,
                    model: template.Model,
                    year: year,
                    color: color,
                    mileage: mileage,
                    fuelType: template.FuelType,
                    purchasePrice: Money.Of(purchase),
                    askingPrice: Money.Of(asking),
                    status: status,
                    dateAddedToInventory: now.AddDays(-daysAgo),
                    nowUtc: now,
                    createdByUserId: null,
                    stockNumber: stockNumber,
                    dealershipId: dealer.Id);

                vehicle.ClearDomainEvents();
                vehiclesToAdd.Add(vehicle);
            }
        }

        if (vehiclesToAdd.Count > 0)
        {
            // Batch insert
            await db.Vehicles.AddRangeAsync(vehiclesToAdd, ct);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} new vehicles across {Dealerships} dealerships. Total inventory: {Total}",
                vehiclesToAdd.Count, dealerships.Count, existingCount + vehiclesToAdd.Count);
        }
    }

    private static string GenerateUniqueVin(int counter, string make, HashSet<string> existingVins)
    {
        var wmi = make switch
        {
            "Tesla" => "5YJ",
            "Ford" => "1FT",
            "Chevrolet" or "GMC" => "1GC",
            "BMW" => "WBA",
            "Mercedes" => "WDB",
            "Audi" => "WAU",
            "Porsche" => "WP0",
            "Toyota" or "Lexus" => "JTD",
            "Honda" => "1HG",
            "Hyundai" or "Kia" => "KMH",
            "Subaru" => "JF1",
            "Mazda" => "JM1",
            "Nissan" or "Infiniti" => "JN1",
            _ => "1HD"
        };

        while (true)
        {
            var candidate = $"{wmi}3E1EB9RA{counter:D6}";
            if (existingVins.Add(candidate))
            {
                return candidate;
            }
            counter++;
        }
    }
}
