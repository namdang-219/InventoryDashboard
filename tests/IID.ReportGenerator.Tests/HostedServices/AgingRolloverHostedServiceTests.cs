using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.ReportGenerator.Configurations;
using IID.ReportGenerator.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace IID.ReportGenerator.Tests.HostedServices;

public class AgingRolloverHostedServiceTests
{
    private readonly Mock<IDashboardSnapshotRefresher> _refresher = new();
    private readonly DashboardBootstrapReadiness _readiness = new();

    private AgingRolloverHostedService CreateSut()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _refresher.Object);
        var provider = services.BuildServiceProvider();

        return new AgingRolloverHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            _readiness,
            Options.Create(new ReportGeneratorOptions { AgingRefreshMinutes = 0 }),
            NullLogger<AgingRolloverHostedService>.Instance);
    }

    [Fact]
    public async Task DoesNotRefresh_WhileBootstrapNotReady()
    {
        var sut = CreateSut();

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await sut.StopAsync(CancellationToken.None);

        _refresher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Stop_BeforeReady_CompletesWithoutError()
    {
        var sut = CreateSut();
        await sut.StartAsync(CancellationToken.None);

        var act = () => sut.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
