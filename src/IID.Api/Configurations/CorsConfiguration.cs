namespace IID.Api.Configurations;

/// <summary>
/// Strongly-typed CORS settings bound from configuration section "Cors".
/// </summary>
public sealed class CorsConfiguration
{
    public const string Name = "Cors";
    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();
}
