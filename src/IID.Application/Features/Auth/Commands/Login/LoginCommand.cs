using MediatR;

namespace IID.Application.Auth.Commands.Login;

/// <summary>
/// Authenticates a user with email + password and returns a signed JWT.
/// </summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;

public sealed record LoginResponse(
    string Token,
    string RefreshToken,
    DateTime RefreshTokenExpiry,
    UserDto User);

public sealed record UserDto(
    string Id,
    string Email,
    IReadOnlyList<string> Roles);
