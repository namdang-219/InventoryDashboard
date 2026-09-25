using FluentAssertions;
using FluentValidation;
using IID.Application.Auth.Commands.Login;
using IID.Application.Common.Behaviors;
using IID.Domain.Common;
using MediatR;
using Xunit;

namespace IID.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    private static LoginResponse CreateFakeResponse() =>
        new("token", "refresh", DateTime.UtcNow, new UserDto("1", "admin@example.com", ["Admin"]));

    [Fact]
    public async Task Handle_Should_CallNext_WhenNoValidators()
    {
        var behavior = new ValidationBehavior<LoginCommand, Result<LoginResponse>>([]);
        var command = new LoginCommand("admin@example.com", "Password123!");
        var expected = Result<LoginResponse>.Success(CreateFakeResponse());

        var actual = await behavior.Handle(command, _ => Task.FromResult(expected), CancellationToken.None);

        actual.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_Should_CallNext_WhenValidationPasses()
    {
        var validators = new IValidator<LoginCommand>[] { new LoginCommandValidator() };
        var behavior = new ValidationBehavior<LoginCommand, Result<LoginResponse>>(validators);
        var command = new LoginCommand("admin@example.com", "Password123!");
        var expected = Result<LoginResponse>.Success(CreateFakeResponse());

        var actual = await behavior.Handle(command, _ => Task.FromResult(expected), CancellationToken.None);

        actual.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_Should_ReturnValidationFailedResult_WhenValidationFailsForGenericResult()
    {
        var validators = new IValidator<LoginCommand>[] { new LoginCommandValidator() };
        var behavior = new ValidationBehavior<LoginCommand, Result<LoginResponse>>(validators);
        var command = new LoginCommand(string.Empty, "string"); // Empty email
        var nextCalled = false;

        var actual = await behavior.Handle(command, _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result<LoginResponse>.Success(CreateFakeResponse()));
        }, CancellationToken.None);

        nextCalled.Should().BeFalse();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorKind.Should().Be(ErrorKind.ValidationFailed);
        actual.Message.Should().Contain("Email is required.");
    }

    public record DummyCommand(string Text) : IRequest<Result>;
    public class DummyCommandValidator : AbstractValidator<DummyCommand>
    {
        public DummyCommandValidator()
        {
            RuleFor(x => x.Text).NotEmpty().WithMessage("Text cannot be empty.");
        }
    }

    [Fact]
    public async Task Handle_Should_ReturnValidationFailedResult_WhenValidationFailsForNonGenericResult()
    {
        var validators = new IValidator<DummyCommand>[] { new DummyCommandValidator() };
        var behavior = new ValidationBehavior<DummyCommand, Result>(validators);
        var command = new DummyCommand(string.Empty);
        var nextCalled = false;

        var actual = await behavior.Handle(command, _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        }, CancellationToken.None);

        nextCalled.Should().BeFalse();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorKind.Should().Be(ErrorKind.ValidationFailed);
        actual.Message.Should().Be("Text cannot be empty.");
    }
}
