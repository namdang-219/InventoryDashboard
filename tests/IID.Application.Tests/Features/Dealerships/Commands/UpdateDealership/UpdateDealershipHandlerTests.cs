using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Dealerships.Commands.UpdateDealership;
using IID.Domain.Common;
using IID.Domain.Dealerships;
using IID.Domain.Dealerships.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.Dealerships.Commands.UpdateDealership;

public class UpdateDealershipHandlerTests
{
    private static readonly DateTimeOffset Created = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly Mock<IDealershipRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IClock> _clock = new();

    private UpdateDealershipHandler CreateSut(Result<int>? saveResult = null)
    {
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(saveResult ?? Result<int>.Success(1));
        return new UpdateDealershipHandler(
            _repo.Object, _uow.Object, _clock.Object,
            NullLogger<UpdateDealershipHandler>.Instance);
    }

    private static Dealership Existing() =>
        Dealership.Create("Old", "OLD1", "Dallas", "TX", "111", Created);

    [Fact]
    public async Task Handle_Should_UpdateAndSave_WhenFound()
    {
        var dealership = Existing();
        _repo.Setup(r => r.GetByIdAsync(dealership.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dealership);

        var result = await CreateSut().Handle(
            new UpdateDealershipCommand(dealership.Id, " New ", " new1 ", "Austin", " ca ", "222"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dealership.Id);
        dealership.Name.Should().Be("New");
        dealership.Code.Should().Be("NEW1");
        dealership.State.Should().Be("CA");
        dealership.UpdatedAt.Should().Be(Now);
        dealership.CreatedAt.Should().Be(Created);
        dealership.DomainEvents.Should().Contain(e => e is DealershipUpdated);
        _repo.Verify(r => r.Update(dealership), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenMissing()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dealership?)null);

        var result = await CreateSut().Handle(
            new UpdateDealershipCommand(Guid.NewGuid(), "N", "C", "City", "ST", ""),
            CancellationToken.None);

        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        _repo.Verify(r => r.Update(It.IsAny<Dealership>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_PropagateFailure_WhenSaveFails()
    {
        var dealership = Existing();
        _repo.Setup(r => r.GetByIdAsync(dealership.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dealership);
        var sut = CreateSut(Result<int>.Failure(ErrorKind.Conflict, "concurrency"));

        var result = await sut.Handle(
            new UpdateDealershipCommand(dealership.Id, "N", "C", "City", "ST", ""),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        result.Message.Should().Be("concurrency");
    }
}
