using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Vehicles.Queries.ListVehicles;
using IID.Domain.Common;
using IID.Domain.Vehicles;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.Vehicles.Queries.ListVehicles;

public class ListVehiclesHandlerTests
{
    private readonly Mock<IVehicleRepository> _repo = new();
    private readonly Mock<IClock> _clock = new();

    private ListVehiclesHandler CreateSut()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        return new ListVehiclesHandler(_repo.Object, _clock.Object, NullLogger<ListVehiclesHandler>.Instance);
    }

    private static Vehicle MakeVehicle()
    {
        var now = DateTimeOffset.UtcNow;
        return Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(20000m), Money.Of(25000m),
            VehicleStatus.Available, now.AddDays(-10), now);
    }

    [Fact]
    public async Task Handle_Should_ClampLimitTo100()
    {
        _repo.Setup(r => r.ListAsync(
                It.Is<VehicleListFilter>(f => f.Limit == 100),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle>(), 0));

        await CreateSut().Handle(new ListVehiclesQuery(null, null, null, null, null, Limit: 9999), CancellationToken.None);

        _repo.Verify();
    }

    [Fact]
    public async Task Handle_Should_ClampLimitToMin1()
    {
        _repo.Setup(r => r.ListAsync(
                It.Is<VehicleListFilter>(f => f.Limit == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle>(), 0));

        await CreateSut().Handle(new ListVehiclesQuery(null, null, null, null, null, Limit: 0), CancellationToken.None);

        _repo.Verify();
    }

    [Fact]
    public async Task Handle_Should_ClampPageToMin1()
    {
        _repo.Setup(r => r.ListAsync(
                It.Is<VehicleListFilter>(f => f.Page == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle>(), 0));

        await CreateSut().Handle(new ListVehiclesQuery(null, null, null, null, null, Page: -5), CancellationToken.None);

        _repo.Verify();
    }

    [Fact]
    public async Task Handle_Should_MapVehicle_ToVehicleResponse()
    {
        var vehicle = MakeVehicle();
        _repo.Setup(r => r.ListAsync(
                It.IsAny<VehicleListFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle> { vehicle }, 1));

        var result = await CreateSut().Handle(new ListVehiclesQuery(null, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Make.Should().Be("Honda");
        result.Value.Items[0].Vin.Should().Be(vehicle.Vin.Value);
        result.Value.Items[0].PurchasePriceAmount.Should().Be(20000m);
        result.Value.Total.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ForwardFilters_ToRepository()
    {
        _repo.Setup(r => r.ListAsync(
                It.Is<VehicleListFilter>(f => f.Make == "Honda" && f.Model == "Civic" && f.MinAgeDays == 30 && f.MaxAgeDays == 90 && f.Status == VehicleStatus.Available),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle>(), 0));

        await CreateSut().Handle(
            new ListVehiclesQuery(Make: "Honda", Model: "Civic", MinAgeDays: 30, MaxAgeDays: 90, Status: VehicleStatus.Available),
            CancellationToken.None);

        _repo.Verify();
    }
}
