using FluentValidation.TestHelper;
using IID.Application.Auth.Commands.RefreshToken;
using Xunit;

namespace IID.Application.Tests.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_WhenRefreshTokenProvided()
    {
        var cmd = new RefreshTokenCommand("valid-token");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_WhenRefreshTokenEmpty(string? token)
    {
        var cmd = new RefreshTokenCommand(token!);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.RefreshToken);
    }
}
