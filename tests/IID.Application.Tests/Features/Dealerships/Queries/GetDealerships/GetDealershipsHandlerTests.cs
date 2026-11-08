using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Dealerships.Queries.GetDealerships;
using IID.Domain.Dealerships;
using Moq;

namespace IID.Application.Tests.Features.Dealerships.Queries.GetDealerships;

public class GetDealershipsHandlerTests
{
    private readonly Mock<IDealershipRepository> _repo = new();

    private static Dealership Make(string code) =>
        Dealership.Create($"Name {code}", code, "City", "ST", "555", DateTimeOffset.UtcNow);

    [Fact]
    public async Task Handle_Should_ReturnEmpty_WhenNoDealerships()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Dealership>());
        _repo.Setup(r => r.GetVehicleCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await new GetDealershipsHandler(_repo.Object)
            .Handle(new GetDealershipsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_MapVehicleCounts_AndDefaultMissingToZero()
    {
        var a = Make("A1");
        var b = Make("B1");
        var c = Make("C1");
        _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Dealership> { a, b, c });
        _repo.Setup(r => r.GetVehicleCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>
            {
                [a.Id] = 7,
                [b.Id] = 0,
                [Guid.NewGuid()] = 99 // orphan count, must be ignored
            });

        var result = await new GetDealershipsHandler(_repo.Object)
            .Handle(new GetDealershipsQuery(), CancellationToken.None);

        result.Value!.Select(d => (d.Code, d.VehicleCount))
            .Should().Equal(("A1", 7), ("B1", 0), ("C1", 0));
    }

    [Fact]
    public async Task Handle_Should_PreserveRepositoryOrder_AndMapFields()
    {
        var a = Make("Z9");
        var b = Make("A1");
        _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Dealership> { a, b });
        _repo.Setup(r => r.GetVehicleCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await new GetDealershipsHandler(_repo.Object)
            .Handle(new GetDealershipsQuery(), CancellationToken.None);

        result.Value!.Select(d => d.Id).Should().Equal(a.Id, b.Id);
        result.Value![0].Should().Be(new DealershipDto(a.Id, a.Name, a.Code, a.City, a.State, a.Phone, 0));
    }
}
