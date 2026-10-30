using IID.Application.Common.Interfaces;
using IID.Infrastructure.Messaging;
using IID.Infrastructure.Persistence;
using IID.ReportGenerator.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IID.ReportGenerator.HostedServices;

/// <summary>
/// Initializes Kafka topics, waits for SQL Server schema readiness, and computes
/// initial static dashboard snapshots for Global and every Dealership on startup.
/// </summary>
public sealed class DashboardBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    KafkaTopicProvisioner topicProvisioner,
    DashboardBootstrapReadiness readiness,
    IOptions<ReportGeneratorOptions> options,
    ILogger<DashboardBootstrapHostedService> logger) : BackgroundService
{
    private readonly ReportGeneratorOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("IID.ReportGenerator bootstrap starting...");

        await topicProvisioner.EnsureTopicsExistAsync(stoppingToken);
        await WaitForDatabaseSchemaAsync(stoppingToken);

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var refresher = scope.ServiceProvider.GetRequiredService<IDashboardSnapshotRefresher>();
            var count = await refresher.RefreshAllSnapshotsAsync(publishUpdates: true, stoppingToken);
            logger.LogInformation("IID.ReportGenerator initial snapshot bootstrap completed ({Count} scopes).", count);
        }

        readiness.MarkReady();
    }

    private async Task WaitForDatabaseSchemaAsync(CancellationToken ct)
    {
        var maxAttempts = Math.Max(1, _options.StartupSchemaRetryCount);
        var delay = TimeSpan.FromSeconds(Math.Max(1, _options.StartupSchemaRetryDelaySeconds));

        for (var attempt = 1; attempt <= maxAttempts && !ct.IsCancellationRequested; attempt++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<IidDbContext>();

                var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
                if (pending.Count == 0)
                {
                    logger.LogInformation("Database schema verified ready for IID.ReportGenerator.");
                    return;
                }

                if (attempt == maxAttempts)
                {
                    logger.LogWarning(
                        "Database still has {Count} pending migration(s) after {MaxAttempts} checks; applying migrations from IID.ReportGenerator.",
                        pending.Count,
                        maxAttempts);
                    await db.Database.MigrateAsync(ct);
                    return;
                }

                logger.LogInformation(
                    "Waiting for IID.Api to finish applying {Count} pending migration(s) (attempt {Attempt}/{MaxAttempts})...",
                    pending.Count,
                    attempt,
                    maxAttempts);
            }
            catch (Exception ex) when (attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "SQL Server not ready yet (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}s...",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);
            }

            await Task.Delay(delay, ct);
        }
    }
}

