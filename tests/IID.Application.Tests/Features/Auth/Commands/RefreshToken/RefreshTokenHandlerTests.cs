using FluentAssertions;
using IID.Application.Auth.Commands.Login;
using IID.Application.Auth.Commands.RefreshToken;
using IID.Application.Common.Interfaces;
using IID.Domain.Common;
using Moq;

namespace IID.Application.Tests.Features.Auth.Commands.RefreshToken;

public class RefreshTokenHandlerTests
{
    [Fact]
    public async Task Handle_Should_DelegateToAuthService_AndReturnSuccess()
    {
        var response = new LoginResponse(
            "new-jwt",
            "new-refresh",
            DateTime.UtcNow.AddDays(7),
            new UserDto("u1", "a@b.com", new[] { "Admin" }));
        var authService = new Mock<IAuthService>();
        authService.Setup(s => s.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Success(response));

        var handler = new RefreshTokenHandler(authService.Object);
        var result = await handler.Handle(new RefreshTokenCommand("refresh-xyz"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("new-jwt");
        authService.Verify(s => s.RefreshTokenAsync("refresh-xyz", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_PropagateFailure_FromAuthService()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(s => s.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure(ErrorKind.Unauthorized, "expired"));

        var handler = new RefreshTokenHandler(authService.Object);
        var result = await handler.Handle(new RefreshTokenCommand("stale"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.Unauthorized);
        result.Message.Should().Be("expired");
    }
}
