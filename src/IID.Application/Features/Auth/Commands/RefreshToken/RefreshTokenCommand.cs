using IID.Application.Auth.Commands.Login;
using MediatR;

namespace IID.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Exchanges a valid refresh token for a new JWT + refresh token pair.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<LoginResponse>>;
