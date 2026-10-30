using FluentAssertions;
using IID.Domain.Dashboard;

namespace IID.Domain.Tests.Dashboard;

public class DashboardSnapshotTests
{
    [Fact]
    public void BuildScopeKey_Should_ReturnGlobal_WhenNullOrEmpty()
    {
        DashboardSnapshot.BuildScopeKey(null).Should().Be("global");
        DashboardSnapshot.BuildScopeKey(Guid.Empty).Should().Be("global");
    }

    [Fact]
    public void BuildScopeKey_Should_ReturnDealershipKey_WhenDealershipIdProvided()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        DashboardSnapshot.BuildScopeKey(id).Should().Be("dealership:11111111-2222-3333-4444-555555555555");
    }

    [Fact]
    public void Create_And_Update_Should_IncrementVersionAndPersistPayloads()
    {
        var t0 = new DateTimeOffset(2026, 10, 29, 10, 0, 0, TimeSpan.Zero);
        var t1 = t0.AddMinutes(5);
        var dealerId = Guid.NewGuid();

        var snapshot = DashboardSnapshot.Create(
            dealerId,
            "{\"total\":1}",
            "{\"items\":[]}",
            "{}",
            "[]",
            "[]",
            "[]",
            "[]",
            t0);

        snapshot.Version.Should().Be(1);
        snapshot.DealershipId.Should().Be(dealerId);
        snapshot.ScopeKey.Should().Be($"dealership:{dealerId:D}");
        snapshot.ComputedAtUtc.Should().Be(t0);

        snapshot.Update(
            "{\"total\":2}",
            "{\"items\":[1]}",
            "{\"charts\":true}",
            "[1]",
            "[]",
            "[2]",
            "[3]",
            t1);

        snapshot.Version.Should().Be(2);
        snapshot.SummaryJson.Should().Be("{\"total\":2}");
        snapshot.LowInventoryAlertsJson.Should().Be("[2]");
        snapshot.RealtimeAlertsJson.Should().Be("[3]");
        snapshot.ComputedAtUtc.Should().Be(t1);
    }
}

