using FluentAssertions;
using IID.Domain.Common;

namespace IID.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_Should_HaveIsSuccessTrue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.ErrorKind.Should().Be(ErrorKind.None);
        result.Message.Should().BeNull();
    }

    [Fact]
    public void Failure_Should_HaveIsSuccessFalse()
    {
        var result = Result<int>.Failure(ErrorKind.NotFound, "not found");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(default(int));
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        result.Message.Should().Be("not found");
    }

    [Fact]
    public void ImplicitOperator_Should_ConvertValueToSuccess()
    {
        Result<int> result = 10;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Fact]
    public void NonGeneric_Success_Should_HaveNoError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.None);
    }

    [Fact]
    public void NonGeneric_Failure_Should_HaveError()
    {
        var result = Result.Failure(ErrorKind.Conflict, "duplicate");

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        result.Message.Should().Be("duplicate");
    }
}
