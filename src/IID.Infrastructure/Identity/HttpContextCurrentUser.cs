using System.Security.Claims;
using IID.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace IID.Infrastructure.Identity;

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? Id => accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? accessor.HttpContext?.User?.FindFirst("sub")?.Value;
    public string? Email => accessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
        ?? accessor.HttpContext?.User?.FindFirst("email")?.Value;
    public string? UserName => accessor.HttpContext?.User?.Identity?.Name
        ?? Email
        ?? Id;
    public bool IsInRole(string role) => accessor.HttpContext?.User?.IsInRole(role) ?? false;
}
