namespace IID.Api.Configurations;

/// <summary>
/// Strongly-typed Swagger settings bound from configuration section "Swagger".
/// </summary>
public sealed class SwaggerConfiguration
{
    public const string Name = "Swagger";
    public string Title { get; init; } = "Intelligent Inventory Dashboard API";
    public string Version { get; init; } = "v1";
}
