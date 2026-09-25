using IID.Application.Auth.Commands.Login;
using IID.Application.Common.Interfaces;
using IID.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using IID.Infrastructure.Identity;

namespace IID.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IAuthService"/> using ASP.NET Identity.
/// Lives in Infrastructure so it can reference <c>SignInManager</c> / <c>UserManager</c>.
/// </summary>
public sealed class AuthService(
    SignInManager<ApplicationUser> signIn,
    UserManager<ApplicationUser> users,
    ITokenService tokenService,
    IOptions<JwtConfiguration> jwtOptions)
    : IAuthService
{
    public async Task<IID.Domain.Common.Result<LoginResponse>> LoginAsync(
        LoginCommand cmd, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(cmd.Email);
        if (user is null)
            return IID.Domain.Common.Result<LoginResponse>.Failure(
                IID.Domain.Common.ErrorKind.Unauthorized, "Invalid email or password.");

        var pwCheck = await signIn.CheckPasswordSignInAsync(user, cmd.Password, lockoutOnFailure: true);
        if (pwCheck.IsLockedOut)
            return IID.Domain.Common.Result<LoginResponse>.Failure(
                IID.Domain.Common.ErrorKind.Unauthorized, "Account is locked. Try again later.");
        if (!pwCheck.Succeeded)
            return IID.Domain.Common.Result<LoginResponse>.Failure(
                IID.Domain.Common.ErrorKind.Unauthorized, "Invalid email or password.");

        var roles = await users.GetRolesAsync(user);
        var token = tokenService.IssueToken(user.Id, user.Email!, roles.ToList());

        // Generate and persist refresh token
        var refreshToken = tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpiryDays);
        await users.UpdateAsync(user);

        return IID.Domain.Common.Result<LoginResponse>.Success(
            new LoginResponse(
                Token: token,
                RefreshToken: refreshToken,
                RefreshTokenExpiry: user.RefreshTokenExpiry!.Value,
                User: new UserDto(
                    Id: user.Id,
                    Email: user.Email!,
                    Roles: roles.ToList())));
    }

    public async Task<IID.Domain.Common.Result<LoginResponse>> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default)
    {
        var user = await users.Users.FirstOrDefaultAsync(
            u => u.RefreshToken == refreshToken, ct);

        if (user is null)
            return IID.Domain.Common.Result<LoginResponse>.Failure(
                ErrorKind.Unauthorized, "Invalid refresh token.");

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
            return IID.Domain.Common.Result<LoginResponse>.Failure(
                ErrorKind.Unauthorized, "Refresh token has expired.");

        var roles = await users.GetRolesAsync(user);
        var newToken = tokenService.IssueToken(user.Id, user.Email!, roles.ToList());
        var newRefreshToken = tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpiryDays);
        await users.UpdateAsync(user);

        return IID.Domain.Common.Result<LoginResponse>.Success(
            new LoginResponse(
                Token: newToken,
                RefreshToken: newRefreshToken,
                RefreshTokenExpiry: user.RefreshTokenExpiry.Value,
                User: new UserDto(
                    Id: user.Id,
                    Email: user.Email!,
                    Roles: roles.ToList())));
    }
}
