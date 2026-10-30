namespace IID.ReportGenerator.HostedServices;

/// <summary>
/// Coordinates startup ordering so Kafka consumers and periodic timers begin
/// only after initial database bootstrap completes.
/// </summary>
public sealed class DashboardBootstrapReadiness
{
    private readonly TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void MarkReady() => _readyTcs.TrySetResult();

    public Task WaitUntilReadyAsync(CancellationToken ct) => _readyTcs.Task.WaitAsync(ct);
}

