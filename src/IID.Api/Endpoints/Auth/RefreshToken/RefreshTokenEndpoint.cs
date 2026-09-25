using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.Auth.Commands.RefreshToken;
using IID.Application.Auth.Commands.Login;
using MediatR;

namespace IID.Api.Endpoints.Auth.RefreshToken;

/// <summary>
/// POST /api/v1/auth/refresh — exchanges a refresh token for a new JWT pair.
/// </summary>
public sealed class RefreshTokenEndpoint(ISender sender) : Endpoint<RefreshTokenRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/v1/auth/refresh");
        AllowAnonymous();  // Refresh token is the credentials here
        Description(x => x.WithTags("Auth"));
        Summary(s =>
        {
            s.Description = "Exchange a valid refresh token for a new JWT + refresh token.";
            s.Response<LoginResponse>(200, "New JWT + refresh token");
        });
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new RefreshTokenCommand(req.RefreshToken), ct);

        if (!result.IsSuccess)
        {
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(result.Value!, ct);
    }
}

public sealed record RefreshTokenRequest(string RefreshToken);
