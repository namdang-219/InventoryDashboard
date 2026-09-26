using FluentAssertions;
using IID.Domain.Common;
using IID.Domain.Vehicles;

namespace IID.Domain.Tests.Common;

public class EntityTests
{
    private sealed class TestEntity : Entity
    {
        public TestEntity(Guid id) { Id = id; }
    }

    [Fact]
    public void Equals_Should_ReturnTrue_WhenSameIdAndType()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_WhenDifferentIds()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        a.Equals(b).Should().BeFalse();
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_WhenDifferentTypes()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new OtherTestEntity(id);

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_WhenComparingToNull()
    {
        var a = new TestEntity(Guid.NewGuid());

        a.Equals(null).Should().BeFalse();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_ReturnTrue_WhenSameReference()
    {
        var a = new TestEntity(Guid.NewGuid());

        a.Equals(a).Should().BeTrue();
        ReferenceEquals(a, a).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_Should_MatchId()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);

        a.GetHashCode().Should().Be(id.GetHashCode());
    }

    [Fact]
    public void Equals_Should_ReturnFalse_WhenIdIsEmpty()
    {
        // Two entities with Guid.Empty must not be considered equal — that
        // would let two transient (unsaved) rows collide.
        var a = new TestEntity(Guid.Empty);
        var b = new TestEntity(Guid.Empty);

        a.Equals(b).Should().BeFalse();
    }

    private sealed class OtherTestEntity : Entity
    {
        public OtherTestEntity(Guid id) { Id = id; }
    }
}

public class AggregateRootTests
{
    private sealed class TestAggregate : AggregateRoot
    {
        public void Raise(IDomainEvent ev) => RaiseDomainEvent(ev);
    }

    private sealed class TestEvent : IDomainEvent
    {
        public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
    }

    [Fact]
    public void DomainEvents_Should_BeEmpty_Initially()
    {
        var agg = new TestAggregate();

        agg.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RaiseDomainEvent_Should_AppendEvent()
    {
        var agg = new TestAggregate();
        var ev = new TestEvent();

        agg.Raise(ev);

        agg.DomainEvents.Should().ContainSingle().Which.Should().BeSameAs(ev);
    }

    [Fact]
    public void ClearDomainEvents_Should_RemoveAll()
    {
        var agg = new TestAggregate();
        agg.Raise(new TestEvent());
        agg.Raise(new TestEvent());

        agg.ClearDomainEvents();

        agg.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_Should_BeReadOnly()
    {
        var agg = new TestAggregate();

        agg.DomainEvents.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }
}
