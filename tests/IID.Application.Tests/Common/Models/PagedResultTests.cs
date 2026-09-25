using FluentAssertions;
using IID.Application.Common.Models;

namespace IID.Application.Tests.Common.Models;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(99, 20, 5)]
    [InlineData(100, 20, 5)]
    [InlineData(101, 20, 6)]
    public void TotalPages_Should_CeilDivideTotalByLimit(int total, int limit, int expectedPages)
    {
        var paged = new PagedResult<int>(Array.Empty<int>(), 1, limit, total);

        paged.TotalPages.Should().Be(expectedPages);
    }
}
