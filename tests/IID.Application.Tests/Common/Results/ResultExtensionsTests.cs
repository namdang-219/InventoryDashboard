using FluentAssertions;
using IID.Application.Common.Results;
using IID.Domain.Common;

namespace IID.Application.Tests.Common.Results;

public class ResultExtensionsTests
{
    [Theory]
    [InlineData(ErrorKind.None, 200)]
    [InlineData(ErrorKind.ValidationFailed, 422)]
    [InlineData(ErrorKind.NotFound, 404)]
    [InlineData(ErrorKind.Conflict, 409)]
    [InlineData(ErrorKind.Unauthorized, 401)]
    [InlineData(ErrorKind.Forbidden, 403)]
    [InlineData(ErrorKind.Internal, 500)]
    public void ToStatusCode_Should_MapEveryErrorKind(ErrorKind kind, int expected)
    {
        kind.ToStatusCode().Should().Be(expected);
    }
}
