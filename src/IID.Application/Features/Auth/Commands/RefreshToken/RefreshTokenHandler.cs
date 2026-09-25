using IID.Application.Auth.Commands.RefreshToken;
using IID.Application.Common.Interfaces;
using MediatR;

namespace IID.Application.Auth.Commands.Login;

/// <summary>
/// Delegates to <see cref="IAuthService"/> so the Application layer has no
/// infrastructure dependency.
/// </summary>
public sealed class RefreshTokenHandler(IAuthService authService)
    : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
        => await authService.RefreshTokenAsync(cmd.RefreshToken, ct);
}
