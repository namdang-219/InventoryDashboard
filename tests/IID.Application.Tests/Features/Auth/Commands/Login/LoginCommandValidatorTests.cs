using FluentAssertions;
using FluentValidation.TestHelper;
using IID.Application.Auth.Commands.Login;

namespace IID.Application.Tests.Features.Auth.Commands.Login;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_WhenEmailAndPasswordProvided()
    {
        var cmd = new LoginCommand("user@example.com", "secret123");

        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_WhenEmailEmpty()
    {
        var cmd = new LoginCommand("", "secret");

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nouser.com")]
    public void Validate_Should_Fail_WhenEmailMalformed(string email)
    {
        var cmd = new LoginCommand(email, "secret");

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_Should_Fail_WhenPasswordEmpty()
    {
        var cmd = new LoginCommand("user@example.com", "");

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Password);
    }
}
