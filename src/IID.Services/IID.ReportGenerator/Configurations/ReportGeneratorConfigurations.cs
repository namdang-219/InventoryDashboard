namespace IID.ReportGenerator.Configurations;

public sealed class ReportGeneratorOptions
{
    public const string Name = "DashboardSnapshots";

    /// <summary>
    /// Periodic interval (in minutes) to refresh all snapshots so time-based aging metrics stay accurate.
    /// </summary>
    public int AgingRefreshMinutes { get; init; } = 60;

    /// <summary>
    /// Maximum startup attempts while waiting for database schema readiness.
    /// </summary>
    public int StartupSchemaRetryCount { get; init; } = 30;

    /// <summary>
    /// Delay (in seconds) between database schema readiness checks on startup.
    /// </summary>
    public int StartupSchemaRetryDelaySeconds { get; init; } = 3;
}

public sealed class OpenObserveConfiguration
{
    public const string Name = "OpenObserve";

    public bool Enabled { get; init; }
    public string Endpoint { get; init; } = string.Empty;
    public string? AuthToken { get; init; }
    public string StreamName { get; init; } = "iid-report-generator";
    public string ServiceName { get; init; } = "IID.ReportGenerator";
    public string ServiceVersion { get; init; } = "1.0.0";
    public string Environment { get; init; } = "Production";
}

