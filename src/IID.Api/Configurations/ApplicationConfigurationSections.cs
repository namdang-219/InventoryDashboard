using IID.Infrastructure.Configuration;

namespace IID.Api.Configurations;

/// <summary>
/// Aggregate configuration constants grouped under a single namespace for traceability.
/// </summary>
public static class ApplicationConfigurationSections
{
    public const string ConnectionStrings = "ConnectionStrings:IID";
    public const string Jwt = JwtConfiguration.Name;
    public const string Swagger = SwaggerConfiguration.Name;
    public const string Cors = CorsConfiguration.Name;
    public const string OpenObserve = OpenObserveConfiguration.Name;
}
