namespace IID.Application.Common.Interfaces;

/// <summary>
/// Issues signed JWT tokens for authenticated users.
/// </summary>
public interface ITokenService
{
    /// <summary>Creates a JWT for the given user claims. Returns the raw token string.</summary>
    string IssueToken(string userId, string email, IReadOnlyList<string> roles);

    /// <summary>Returns the token expiry as an absolute UTC instant.</summary>
    DateTimeOffset ExpiresAtUtc();

    /// <summary>Generates a cryptographically secure refresh token.</summary>
    string GenerateRefreshToken();
}
