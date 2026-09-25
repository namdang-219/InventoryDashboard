using FastEndpoints;
using FastEndpoints.Swagger;
using IID.Infrastructure.Persistence;
using IID.Infrastructure.Realtime;
using Serilog;

namespace IID.Api.Extensions;

/// <summary>
/// Application pipeline mapping, mirroring Sportcast's style.
/// Keeps <c>Program.cs</c> to a single <c>app.MapApi()</c> call.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Runs pending EF migrations and idempotent data seeders before the
    /// pipeline starts handling traffic. Failures are logged but do not
    /// abort startup; the app will still come up so the operator can see
    /// the error in logs.
    /// </summary>
    public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IidDbInitializer>();
        await initializer.InitializeAsync();
        return app;
    }

    public static WebApplication MapApi(this WebApplication app)
    {
        // Per-request observability
        app.UseSerilogRequestLogging();

        // Standard middleware
        app.UseCors("default");

        app.UseAuthentication();
        app.UseAuthorization();

        // FastEndpoints routing
        app.UseFastEndpoints();

        // OpenAPI spec + Swagger UI
        app.UseSwaggerGen();

        // SignalR hub
        app.MapHub<InventoryHub>("/hubs/inventory");

        return app;
    }
}
