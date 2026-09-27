using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using MediatR;

namespace IID.Application.Auth.Commands.Login;

/// <summary>
/// Delegates to <see cref="IAuthService"/> so the Application layer has no
/// infrastructure dependency (workspace rule: zero infrastructure refs in Application).
/// </summary>
public sealed class LoginHandler(
    IAuthService authService,
    ILogger<LoginHandler> logger) : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var result = await authService.LoginAsync(cmd, ct);

        if (result.IsSuccess)
        {
            logger.LoginSucceeded(cmd.Email, result.Value!.User.Id);
        }
        else
        {
            logger.LoginFailed(cmd.Email, result.Message ?? "Unknown error");
        }

        return result;
    }
}
