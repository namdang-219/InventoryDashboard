using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Dealerships.Commands.CreateDealership;
using IID.Domain.Common;
using IID.Domain.Dealerships;
using IID.Domain.Dealerships.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.Dealerships.Commands.CreateDealership;

public class CreateDealershipHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 8, 0, 0, TimeSpan.Zero);

    private readonly Mock<IDealershipRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IClock> _clock = new();

    private CreateDealershipHandler CreateSut(Result<int>? saveResult = null)
    {
        _clock.SetupGet(c => c.UtcNow).Returns(Now);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(saveResult ?? Result<int>.Success(1));
        return new CreateDealershipHandler(
            _repo.Object, _uow.Object, _clock.Object,
            NullLogger<CreateDealershipHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_PersistNormalizedDealership_WhenValid()
    {
        Dealership? added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Dealership>(), It.IsAny<CancellationToken>()))
            .Callback<Dealership, CancellationToken>((d, _) => added = d)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().Handle(
            new CreateDealershipCommand("  Downtown  ", " dt01 ", " Austin ", " tx ", " 555 "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added.Should().NotBeNull();
        result.Value.Should().Be(added!.Id);
        added.Name.Should().Be("Downtown");
        added.Code.Should().Be("DT01");
        added.State.Should().Be("TX");
        added.CreatedAt.Should().Be(Now);
        added.DomainEvents.Should().ContainSingle(e => e is DealershipAdded);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_PropagateFailure_WhenSaveFails()
    {
        var sut = CreateSut(Result<int>.Failure(ErrorKind.Conflict, "duplicate code"));

        var result = await sut.Handle(
            new CreateDealershipCommand("Downtown", "DT01", "Austin", "TX", ""),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        result.Message.Should().Be("duplicate code");
    }

    [Fact]
    public async Task Handle_Should_Throw_WhenNameBlank_AndNotSave()
    {
        var sut = CreateSut();

        var act = () => sut.Handle(
            new CreateDealershipCommand(" ", "DT01", "Austin", "TX", ""),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.AddAsync(It.IsAny<Dealership>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
