using IID.Application.Auth.Commands.Login;
using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using MediatR;

namespace IID.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Delegates to <see cref="IAuthService"/> so the Application layer has no
/// infrastructure dependency.
/// </summary>
public sealed class RefreshTokenHandler(
    IAuthService authService,
    ILogger<RefreshTokenHandler> logger)
    : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(cmd.RefreshToken, ct);

        if (result.IsSuccess)
        {
            logger.RefreshTokenSucceeded(result.Value!.User.Id);
        }
        else
        {
            logger.RefreshTokenFailed(result.Message ?? "Unknown error");
        }

        return result;
    }
}
