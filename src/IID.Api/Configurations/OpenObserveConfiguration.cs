namespace IID.Api.Configurations;

/// <summary>
/// Strongly-typed OpenObserve settings bound from configuration section "OpenObserve".
/// </summary>
public sealed class OpenObserveConfiguration
{
    public const string Name = "OpenObserve";

    public bool Enabled { get; init; }
    public string Endpoint { get; init; } = String.Empty;
    public string? AuthToken { get; init; }
    public string StreamName { get; init; } = "iid-api";
    public string ServiceName { get; init; } = "IID.Api";
    public string ServiceVersion { get; init; } = "1.0.0";
    public string Environment { get; init; } = "Production";
}
