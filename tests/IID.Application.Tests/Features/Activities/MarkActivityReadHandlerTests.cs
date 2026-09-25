using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Activities.Commands.MarkActivityRead;
using Moq;

namespace IID.Application.Tests.Features.Activities;

public class MarkActivityReadHandlerTests
{
    private readonly Mock<IUserActivityReadRepository> _userActivityReadRepositoryMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUserMock.Setup(u => u.Id).Returns((string?)null);

        var handler = new MarkActivityReadHandler(_userActivityReadRepositoryMock.Object, _currentUserMock.Object);
        var result = await handler.Handle(new MarkActivityReadCommand(null, true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(Domain.Common.ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenMarkAll_CallsMarkAllAsRead()
    {
        var userId = "user-123";
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var handler = new MarkActivityReadHandler(_userActivityReadRepositoryMock.Object, _currentUserMock.Object);
        var result = await handler.Handle(new MarkActivityReadCommand(null, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userActivityReadRepositoryMock.Verify(r => r.MarkAllAsReadAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSpecificIds_CallsMarkAsRead()
    {
        var userId = "user-123";
        var ids = new List<string> { "act-1", "act-2" };
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var handler = new MarkActivityReadHandler(_userActivityReadRepositoryMock.Object, _currentUserMock.Object);
        var result = await handler.Handle(new MarkActivityReadCommand(ids, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userActivityReadRepositoryMock.Verify(r => r.MarkAsReadAsync(userId, ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
