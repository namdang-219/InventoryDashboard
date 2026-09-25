using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Vehicles.Queries.GetAgingStock;
using IID.Domain.Vehicles;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.Vehicles.Queries.GetAgingStock;

public class GetAgingStockHandlerTests
{
    private readonly Mock<IVehicleRepository> _repo = new();
    private readonly Mock<IClock> _clock = new();

    private GetAgingStockHandler CreateSut(DateTimeOffset now)
    {
        _clock.SetupGet(c => c.UtcNow).Returns(now);
        return new GetAgingStockHandler(_repo.Object, _clock.Object, NullLogger<GetAgingStockHandler>.Instance);
    }

    private static Vehicle MakeVehicle(int daysOld, DateTimeOffset asOf)
    {
        return Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1000m), Money.Of(2000m),
            VehicleStatus.Available,
            asOf.AddDays(-daysOld), asOf);
    }

    [Fact]
    public async Task Handle_Should_ReturnOnlyAgingVehicles()
    {
        var now = DateTimeOffset.UtcNow;
        var vehicles = new List<Vehicle>
        {
            MakeVehicle(10, now),
            MakeVehicle(95, now),
            MakeVehicle(120, now),
            MakeVehicle(180, now),
        };
        // Handler asks the repo for everything > 91 days, then filters client-side.
        _repo.Setup(r => r.ListAsync(
                It.Is<VehicleListFilter>(f => f.MinAgeDays == 91 && f.Page == 1 && f.Limit == 20 && f.Sort == "dateAdded" && f.Order == "asc"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((vehicles, vehicles.Count));

        var result = await CreateSut(now).Handle(new GetAgingStockQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Total.Should().Be(vehicles.Count);
    }

    [Fact]
    public async Task Handle_Should_ClampPageAndLimit()
    {
        var now = DateTimeOffset.UtcNow;
        _repo.Setup(r => r.ListAsync(
                It.Is<VehicleListFilter>(f => f.MinAgeDays == 91 && f.Page == 1 && f.Limit == 100 && f.Sort == "dateAdded" && f.Order == "asc"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle>(), 0));

        await CreateSut(now).Handle(new GetAgingStockQuery(Page: -3, Limit: 99999), CancellationToken.None);

        _repo.Verify();
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_WhenNoAgingVehicles()
    {
        var now = DateTimeOffset.UtcNow;
        _repo.Setup(r => r.ListAsync(
                It.Is<VehicleListFilter>(f => f.MinAgeDays == 91 && f.Page == 1 && f.Limit == 20 && f.Sort == "dateAdded" && f.Order == "asc"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle>(), 0));

        var result = await CreateSut(now).Handle(new GetAgingStockQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalPages.Should().Be(0);
    }
}
