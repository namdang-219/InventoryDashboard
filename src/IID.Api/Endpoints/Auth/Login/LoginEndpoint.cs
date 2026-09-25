using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.Auth.Commands.Login;
using MediatR;

namespace IID.Api.Endpoints.Auth.Login;

/// <summary>
/// POST /api/v1/auth/login — authenticates a user and returns a JWT.
/// </summary>
public sealed class LoginEndpoint(ISender sender) : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/v1/auth/login");
        AllowAnonymous();  // No auth header required to sign in
        Description(x => x.WithTags("Auth"));
        Summary(s =>
        {
            s.Description = "Authenticate with email and password. Returns a JWT on success.";
            s.Response<LoginResponse>(200, "JWT + user info");
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(req.Email, req.Password), ct);

        if (!result.IsSuccess)
        {
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(result.Value!, ct);
    }
}

public sealed record LoginRequest(string Email, string Password);
