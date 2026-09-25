using FluentAssertions;
using IID.Infrastructure.Persistence;

namespace IID.Infrastructure.Tests.Persistence;

public class SeedOptionsTests
{
    [Fact]
    public void Defaults_Should_RunMigrationsAndSeeders()
    {
        var options = new SeedOptions();

        options.RunMigrations.Should().BeTrue();
        options.RunSeeders.Should().BeTrue();
    }

    [Fact]
    public void Passwords_Should_DefaultToNull()
    {
        var options = new SeedOptions();

        options.AdminPassword.Should().BeNull();
        options.ViewerPassword.Should().BeNull();
    }

    [Fact]
    public void Name_Should_BeSeed()
    {
        SeedOptions.Name.Should().Be("Seed");
    }
}
