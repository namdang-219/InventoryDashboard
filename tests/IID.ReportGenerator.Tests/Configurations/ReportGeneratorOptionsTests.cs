using FluentAssertions;
using IID.ReportGenerator.Configurations;
using Microsoft.Extensions.Configuration;

namespace IID.ReportGenerator.Tests.Configurations;

public class ReportGeneratorOptionsTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var o = new ReportGeneratorOptions();

        o.AgingRefreshMinutes.Should().Be(60);
        o.StartupSchemaRetryCount.Should().Be(30);
        o.StartupSchemaRetryDelaySeconds.Should().Be(3);
    }

    [Fact]
    public void SectionName_IsDashboardSnapshots()
        => ReportGeneratorOptions.Name.Should().Be("DashboardSnapshots");

    [Fact]
    public void Binds_FromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DashboardSnapshots:AgingRefreshMinutes"] = "5",
                ["DashboardSnapshots:StartupSchemaRetryCount"] = "7",
            })
            .Build();

        var o = config.GetSection(ReportGeneratorOptions.Name).Get<ReportGeneratorOptions>()!;

        o.AgingRefreshMinutes.Should().Be(5);
        o.StartupSchemaRetryCount.Should().Be(7);
        o.StartupSchemaRetryDelaySeconds.Should().Be(3);
    }

    [Fact]
    public void OpenObserve_Defaults_AreExpected()
    {
        var o = new OpenObserveConfiguration();

        o.Enabled.Should().BeFalse();
        o.Endpoint.Should().BeEmpty();
        o.AuthToken.Should().BeNull();
        o.StreamName.Should().Be("iid-report-generator");
        o.ServiceName.Should().Be("IID.ReportGenerator");
        o.Environment.Should().Be("Production");
        OpenObserveConfiguration.Name.Should().Be("OpenObserve");
    }
}
