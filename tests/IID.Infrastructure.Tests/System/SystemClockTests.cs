using FluentAssertions;
using IID.Infrastructure.System;

namespace IID.Infrastructure.Tests.System;

public class SystemClockTests
{
    [Fact]
    public void UtcNow_Should_ReturnUtcNow()
    {
        var clock = new SystemClock();
        var before = DateTimeOffset.UtcNow;

        var result = clock.UtcNow;

        var after = DateTimeOffset.UtcNow;
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void UtcNow_Should_HaveOffsetZero()
    {
        var clock = new SystemClock();
        clock.UtcNow.Offset.Should().Be(TimeSpan.Zero);
    }
}
