using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IID.Infrastructure.Identity;

namespace IID.Infrastructure.Persistence;

/// <summary>
/// Composition root for one-time database work that must run on app startup:
/// applies pending EF migrations, then runs each registered seeder in order.
///
/// Each seeder is responsible for being idempotent (skip-if-present). The
/// initializer itself is safe to call repeatedly; it logs and continues past
/// a failure in any single seeder so a partial seed never blocks startup.
/// </summary>
public sealed class IidDbInitializer(
    IidDbContext db,
    IEnumerable<IidSeeder> seeders,
    IOptions<SeedOptions> options,
    ILogger<IidDbInitializer> logger)
{
    private readonly IidDbContext _db = db;
    private readonly IidSeeder[] _seeders = seeders.ToArray();
    private readonly SeedOptions _options = options.Value;
    private readonly ILogger<IidDbInitializer> _logger = logger;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await MigrateAsync(ct);
        await SeedAsync(ct);
    }

    private async Task MigrateAsync(CancellationToken ct)
    {
        if (!_options.RunMigrations)
        {
            _logger.LogInformation("Skipping EF migrations (Seed:RunMigrations=false).");
            return;
        }

        _logger.LogInformation("Applying pending EF migrations…");

        // MigrateAsync will itself create the database if it doesn't exist
        // (it executes "CREATE DATABASE" before applying the migration script),
        // so EnsureCreatedAsync is unnecessary — and in fact incompatible
        // because EnsureCreated creates the schema from the model snapshot,
        // bypassing __EFMigrationsHistory, which then breaks MigrateAsync.
        var pending = await _db.Database.GetPendingMigrationsAsync(ct);
        if (!pending.Any())
        {
            _logger.LogInformation("Database schema is up to date.");
            return;
        }

        await _db.Database.MigrateAsync(ct);
        _logger.LogInformation("Applied {Count} migration(s).", pending.Count());
    }

    private async Task SeedAsync(CancellationToken ct)
    {
        if (!_options.RunSeeders)
        {
            _logger.LogInformation("Skipping data seeders (Seed:RunSeeders=false).");
            return;
        }

        // Stable order so seeders can build on each other
        // (Identity before Vehicles before VehicleActions).
        var ordered = _seeders
            .OrderBy(s => s.Order)
            .ThenBy(s => s.GetType().Name, StringComparer.Ordinal);

        foreach (var seeder in ordered)
        {
            try
            {
                await seeder.SeedAsync(ct);
                _logger.LogInformation("Seeder {Seeder} completed.", seeder.GetType().Name);
            }
            catch (Exception ex)
            {
                // Never block startup over a bad seed row.
                _logger.LogError(ex, "Seeder {Seeder} failed; continuing startup.", seeder.GetType().Name);
            }
        }
    }
}

/// <summary>
/// Marker interface for a single self-contained seed step.
/// Implementations MUST be idempotent: re-running must not duplicate data.
/// </summary>
public interface IidSeeder
{
    /// <summary>Lower runs first. Recommended: 10=identity, 20=vehicles, 30=actions.</summary>
    int Order { get; }

    Task SeedAsync(CancellationToken ct = default);
}

/// <summary>
/// Bound from configuration section "Seed".
/// All flags default to true (auto-on-startup behaviour the user picked).
/// </summary>
public sealed class SeedOptions
{
    public const string Name = "Seed";

    /// <summary>Apply pending EF migrations on startup. Default true.</summary>
    public bool RunMigrations { get; set; } = true;

    /// <summary>Run data seeders on startup. Default true.</summary>
    public bool RunSeeders { get; set; } = true;

    /// <summary>Override the admin password seeded in Identity. Leave null to use the dev default.</summary>
    public string? AdminPassword { get; set; }

    /// <summary>Override the viewer password seeded in Identity. Leave null to use the dev default.</summary>
    public string? ViewerPassword { get; set; }
}
