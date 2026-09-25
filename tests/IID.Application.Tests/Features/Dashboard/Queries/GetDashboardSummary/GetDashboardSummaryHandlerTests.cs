using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Dtos;
using IID.Application.Dashboard.Queries.GetDashboardSummary;
using IID.Domain.Common;
using IID.Domain.Vehicles;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryHandlerTests
{
    private readonly Mock<IVehicleRepository> _repo = new();
    private readonly Mock<IClock> _clock = new();

    private GetDashboardSummaryHandler CreateSut(DateTimeOffset now)
    {
        _clock.SetupGet(c => c.UtcNow).Returns(now);
        return new GetDashboardSummaryHandler(_repo.Object, _clock.Object, NullLogger<GetDashboardSummaryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ReturnCorrectCounts()
    {
        var now = DateTimeOffset.UtcNow;
        var vehicles = new List<Vehicle>
        {
            MakeVehicle("Honda", VehicleStatus.Available, now.AddDays(-10)),
            MakeVehicle("Toyota", VehicleStatus.Available, now.AddDays(-20)),
            MakeVehicle("Ford", VehicleStatus.Sold, now.AddDays(-5)),
            MakeVehicle("BMW", VehicleStatus.Wholesale, now.AddDays(-100)),
        };
        _repo.Setup(r => r.ListAsync(It.Is<VehicleListFilter>(f => f.Limit == 1000 && f.Sort == "dateAdded" && f.Order == "desc"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((vehicles, vehicles.Count));

        var result = await CreateSut(now).Handle(new GetDashboardSummaryQuery(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        dto.TotalInventory.Should().Be(4);
        dto.AvailableCount.Should().Be(2);
        dto.SoldCount.Should().Be(1);
        dto.WholesaleCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_IncludeAgingBuckets()
    {
        var now = DateTimeOffset.UtcNow;
        var vehicles = new List<Vehicle>
        {
            MakeVehicle("Honda", VehicleStatus.Available, now.AddDays(-5)),   // none
            MakeVehicle("Toyota", VehicleStatus.Available, now.AddDays(-40)),  // warning
            MakeVehicle("Ford", VehicleStatus.Available, now.AddDays(-70)),    // high
            MakeVehicle("BMW", VehicleStatus.Available, now.AddDays(-100)),    // critical
        };
        _repo.Setup(r => r.ListAsync(It.Is<VehicleListFilter>(f => f.Limit == 1000 && f.Sort == "dateAdded" && f.Order == "desc"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((vehicles, vehicles.Count));

        var result = await CreateSut(now).Handle(new GetDashboardSummaryQuery(null), CancellationToken.None);

        var buckets = result.Value!.AgingBySeverity;
        buckets["None"].Should().Be(1);
        buckets["Warning"].Should().Be(1);
        buckets["High"].Should().Be(1);
        buckets["Critical"].Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptySummary_WhenNoVehicles()
    {
        _repo.Setup(r => r.ListAsync(It.Is<VehicleListFilter>(f => f.Limit == 1000 && f.Sort == "dateAdded" && f.Order == "desc"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle>(), 0));

        var result = await CreateSut(DateTimeOffset.UtcNow).Handle(new GetDashboardSummaryQuery(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalInventory.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_GroupByMake()
    {
        var now = DateTimeOffset.UtcNow;
        var vehicles = new List<Vehicle>
        {
            MakeVehicle("Honda", VehicleStatus.Available, now.AddDays(-5)),
            MakeVehicle("Honda", VehicleStatus.Available, now.AddDays(-10)),
            MakeVehicle("Toyota", VehicleStatus.Available, now.AddDays(-5)),
        };
        _repo.Setup(r => r.ListAsync(It.Is<VehicleListFilter>(f => f.Limit == 1000 && f.Sort == "dateAdded" && f.Order == "desc"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((vehicles, vehicles.Count));

        var result = await CreateSut(now).Handle(new GetDashboardSummaryQuery(null), CancellationToken.None);

        result.Value!.TopMakes.Should().Contain(m => m.Make == "Honda" && m.Count == 2);
    }

    private static Vehicle MakeVehicle(string make, VehicleStatus status, DateTimeOffset dateAdded)
    {
        var now = DateTimeOffset.UtcNow;
        return Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            make, "Model", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1000m), Money.Of(2000m),
            status, dateAdded, now);
    }
}
