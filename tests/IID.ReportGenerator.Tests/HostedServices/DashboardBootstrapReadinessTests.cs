using FluentAssertions;
using IID.ReportGenerator.HostedServices;

namespace IID.ReportGenerator.Tests.HostedServices;

public class DashboardBootstrapReadinessTests
{
    [Fact]
    public async Task WaitUntilReady_DoesNotComplete_BeforeMarkReady()
    {
        var sut = new DashboardBootstrapReadiness();

        var wait = sut.WaitUntilReadyAsync(CancellationToken.None);
        await Task.Delay(50);

        wait.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task WaitUntilReady_Completes_AfterMarkReady()
    {
        var sut = new DashboardBootstrapReadiness();
        var wait = sut.WaitUntilReadyAsync(CancellationToken.None);

        sut.MarkReady();

        await wait.WaitAsync(TimeSpan.FromSeconds(2));
        wait.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task MarkReady_IsIdempotent_AndLateWaitersCompleteImmediately()
    {
        var sut = new DashboardBootstrapReadiness();

        sut.MarkReady();
        var act = () => { sut.MarkReady(); return Task.CompletedTask; };
        await act.Should().NotThrowAsync();

        await sut.WaitUntilReadyAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task WaitUntilReady_Throws_WhenCancelled()
    {
        var sut = new DashboardBootstrapReadiness();
        using var cts = new CancellationTokenSource();

        var wait = sut.WaitUntilReadyAsync(cts.Token);
        cts.Cancel();

        await FluentActions.Awaiting(() => wait).Should().ThrowAsync<OperationCanceledException>();
    }
}
