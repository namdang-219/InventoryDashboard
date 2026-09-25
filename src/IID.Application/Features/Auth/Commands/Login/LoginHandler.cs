using IID.Application.Common.Interfaces;
using MediatR;

namespace IID.Application.Auth.Commands.Login;

/// <summary>
/// Delegates to <see cref="IAuthService"/> so the Application layer has no
/// infrastructure dependency (workspace rule: zero infrastructure refs in Application).
/// </summary>
public sealed class LoginHandler(IAuthService authService) : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand cmd, CancellationToken ct)
        => await authService.LoginAsync(cmd, ct);
}
