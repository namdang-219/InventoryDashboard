namespace IID.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed JWT settings bound from configuration section "Jwt".
/// Lives in Infrastructure so <c>TokenService</c> (also in Infrastructure) can use it
/// without a reference to the API layer.
/// </summary>
public sealed class JwtConfiguration
{
    public const string Name = "Jwt";
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 60;
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
