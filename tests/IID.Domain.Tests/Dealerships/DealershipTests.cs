using IID.Domain.Dealerships;

namespace IID.Domain.Tests.Dealerships;

public sealed class DealershipTests
{
    [Fact]
    public void Create_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var dealership = Dealership.Create(
            "Apex Motors - Downtown",
            "dlr-la-01",
            "Los Angeles",
            "ca",
            "(213) 555-0101",
            now);

        // Assert
        Assert.NotEqual(Guid.Empty, dealership.Id);
        Assert.Equal("Apex Motors - Downtown", dealership.Name);
        Assert.Equal("DLR-LA-01", dealership.Code);
        Assert.Equal("Los Angeles", dealership.City);
        Assert.Equal("CA", dealership.State);
        Assert.Equal("(213) 555-0101", dealership.Phone);
        Assert.Equal(now, dealership.CreatedAt);
        Assert.Equal(now, dealership.UpdatedAt);
    }

    [Theory]
    [InlineData("", "DLR-01")]
    [InlineData("   ", "DLR-01")]
    [InlineData("Valid Name", "")]
    [InlineData("Valid Name", "   ")]
    public void Create_WithInvalidNameOrCode_ThrowsArgumentException(string name, string code)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            Dealership.Create(name, code, "City", "CA", "123", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Update_ModifiesPropertiesAndUpdatesTimestamp()
    {
        // Arrange
        var created = DateTimeOffset.UtcNow.AddDays(-1);
        var updated = DateTimeOffset.UtcNow;
        var dealership = Dealership.Create("Old Name", "CODE1", "Old City", "TX", "111", created);

        // Act
        dealership.Update("New Name", "code2", "New City", "ny", "222", updated);

        // Assert
        Assert.Equal("New Name", dealership.Name);
        Assert.Equal("CODE2", dealership.Code);
        Assert.Equal("New City", dealership.City);
        Assert.Equal("NY", dealership.State);
        Assert.Equal("222", dealership.Phone);
        Assert.Equal(created, dealership.CreatedAt);
        Assert.Equal(updated, dealership.UpdatedAt);
    }
}
