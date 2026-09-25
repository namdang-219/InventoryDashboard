using IID.Application.Auth.Commands.Login;
namespace IID.Application.Common.Interfaces;

/// <summary>
/// Authentication operations.
/// </summary>
public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginCommand cmd, CancellationToken ct = default);
    Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
