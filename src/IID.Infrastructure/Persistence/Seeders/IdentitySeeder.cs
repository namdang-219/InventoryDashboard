using IID.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IID.Infrastructure.Persistence;

/// <summary>
/// Seeds ASP.NET Identity roles (<c>Manager</c>, <c>Viewer</c>) and two default
/// users wired up to those roles. Idempotent: skips any role/user that already
/// exists, so re-running the initializer never duplicates rows.
/// </summary>
public sealed class IdentitySeeder(
    RoleManager<IdentityRole> roles,
    UserManager<ApplicationUser> users,
    IOptions<SeedOptions> options,
    ILogger<IdentitySeeder> logger) : IidSeeder
{
    private const string ManagerRole = "Manager";
    private const string ViewerRole = "Viewer";

    private const string AdminEmail = "admin@iid.local";
    private const string AdminUserName = "admin@iid.local";
    private const string ViewerEmail = "viewer@iid.local";
    private const string ViewerUserName = "viewer@iid.local";

    // Default passwords — DEV ONLY. Override via Seed:AdminPassword / Seed:ViewerPassword
    // (or env vars IID_SEED__ADMINPASSWORD / IID_SEED__VIEWERPASSWORD) in any non-dev env.
    private const string DefaultAdminPassword = "P@ssw0rd!Admin";
    private const string DefaultViewerPassword = "P@ssw0rd!Viewer";

    public int Order => 10;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await EnsureRoleAsync(ManagerRole);
        await EnsureRoleAsync(ViewerRole);

        await EnsureUserAsync(
            email: AdminEmail,
            userName: AdminUserName,
            password: options.Value.AdminPassword ?? DefaultAdminPassword,
            role: ManagerRole);

        await EnsureUserAsync(
            email: ViewerEmail,
            userName: ViewerUserName,
            password: options.Value.ViewerPassword ?? DefaultViewerPassword,
            role: ViewerRole);
    }

    private async Task EnsureRoleAsync(string role)
    {
        if (await roles.RoleExistsAsync(role))
        {
            logger.LogDebug("Role {Role} already exists; skipping.", role);
            return;
        }
        var result = await roles.CreateAsync(new IdentityRole(role));
        if (result.Succeeded)
            logger.LogInformation("Created role {Role}.", role);
        else
            throw new InvalidOperationException($"Failed to create role {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    private async Task EnsureUserAsync(string email, string userName, string password, string role)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            logger.LogDebug("User {Email} already exists; skipping.", email);
            return;
        }

        var user = new ApplicationUser(userName)
        {
            Email = email,
            EmailConfirmed = true,
        };

        var create = await users.CreateAsync(user, password);
        if (!create.Succeeded)
            throw new InvalidOperationException($"Failed to create user {email}: {string.Join(", ", create.Errors.Select(e => e.Description))}");

        var addRole = await users.AddToRoleAsync(user, role);
        if (!addRole.Succeeded)
            throw new InvalidOperationException($"Failed to add role {role} to {email}: {string.Join(", ", addRole.Errors.Select(e => e.Description))}");

        logger.LogInformation("Created user {Email} with role {Role}.", email, role);
    }
}
