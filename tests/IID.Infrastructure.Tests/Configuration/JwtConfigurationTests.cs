using FluentAssertions;
using IID.Infrastructure.Configuration;

namespace IID.Infrastructure.Tests.Configuration;

public class JwtConfigurationTests
{
    [Fact]
    public void DefaultValues_Should_BeSet()
    {
        var config = new JwtConfiguration();

        config.Issuer.Should().BeEmpty();
        config.Audience.Should().BeEmpty();
        config.Key.Should().BeEmpty();
        config.ExpiryMinutes.Should().Be(60);
    }

    [Fact]
    public void Init_Should_SetProperties()
    {
        var config = new JwtConfiguration
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "super-secret-key-that-is-long-enough",
            ExpiryMinutes = 120
        };

        config.Issuer.Should().Be("test-issuer");
        config.Audience.Should().Be("test-audience");
        config.Key.Should().Be("super-secret-key-that-is-long-enough");
        config.ExpiryMinutes.Should().Be(120);
    }

    [Fact]
    public void Name_Should_BeJwt()
    {
        JwtConfiguration.Name.Should().Be("Jwt");
    }
}
