using IID.Application.Common.Interfaces;
using IID.ReportGenerator.Configurations;
using Microsoft.Extensions.Options;

namespace IID.ReportGenerator.HostedServices;

/// <summary>
/// Periodically refreshes all static dashboard snapshots (hourly by default) so
/// time-based metrics (<c>DaysInInventory</c> and <c>AgingSeverity</c>) stay fresh
/// even when no vehicle mutations occur.
/// </summary>
public sealed class AgingRolloverHostedService(
    IServiceScopeFactory scopeFactory,
    DashboardBootstrapReadiness readiness,
    IOptions<ReportGeneratorOptions> options,
    ILogger<AgingRolloverHostedService> logger) : BackgroundService
{
    private readonly ReportGeneratorOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await readiness.WaitUntilReadyAsync(stoppingToken);

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.AgingRefreshMinutes));
        logger.LogInformation("Scheduled aging rollover refresh active (interval={Interval}).", interval);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var refresher = scope.ServiceProvider.GetRequiredService<IDashboardSnapshotRefresher>();
                var count = await refresher.RefreshAllSnapshotsAsync(publishUpdates: true, stoppingToken);
                logger.LogInformation("Scheduled aging rollover refreshed {Count} dashboard snapshot(s).", count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Scheduled aging rollover refresh failed.");
            }
        }
    }
}

