using IID.Application.Auth.Commands.Login;
namespace IID.Application.Common.Interfaces;

/// <summary>
/// Authentication operations. Lives in Application so the endpoint has no
/// infrastructure dependency. Implementation is in Infrastructure.
/// </summary>
public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginCommand cmd, CancellationToken ct = default);
    Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
