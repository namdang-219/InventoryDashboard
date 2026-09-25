using FastEndpoints;
using FluentAssertions;
using IID.Api.Endpoints.Auth;
using IID.Application.Auth.Commands.Login;
using IID.Domain.Common;
using MediatR;
using Moq;
using Xunit;

namespace IID.Api.Tests.Endpoints;

public class LoginEndpointTests
{
    [Fact]
    public async Task HandleAsync_Should_Return422_WhenValidationFails()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure(ErrorKind.ValidationFailed, "Email is required."));

        var ep = Factory.Create<LoginEndpoint>(sender.Object);
        await ep.HandleAsync(new LoginRequest(string.Empty, "string"), CancellationToken.None);

        ep.HttpContext.Response.StatusCode.Should().Be(422);
        ep.ValidationFailures.Should().ContainSingle(f => f.ErrorMessage == "Email is required.");
    }
}
