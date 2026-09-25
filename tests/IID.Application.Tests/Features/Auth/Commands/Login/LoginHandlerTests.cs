using FluentAssertions;
using IID.Application.Auth.Commands.Login;
using IID.Application.Common.Interfaces;
using IID.Domain.Common;
using Moq;

namespace IID.Application.Tests.Features.Auth.Commands.Login;

public class LoginHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenCredentialsValid()
    {
        var response = new LoginResponse("jwt-token", "refresh", DateTime.UtcNow.AddDays(7),
            new UserDto("u1", "a@b.com", new[] { "Admin" }));
        var authService = new Mock<IAuthService>();
        authService.Setup(s => s.LoginAsync(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Success(response));

        var handler = new LoginHandler(authService.Object);
        var result = await handler.Handle(new LoginCommand("a@b.com", "pass"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task Handle_Should_DelegateToAuthService()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(s => s.LoginAsync(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure(ErrorKind.Unauthorized, "Invalid"));

        var handler = new LoginHandler(authService.Object);
        var result = await handler.Handle(new LoginCommand("a@b.com", "bad"), CancellationToken.None);

        result.ErrorKind.Should().Be(ErrorKind.Unauthorized);
        authService.Verify(s => s.LoginAsync(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
