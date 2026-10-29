using FastEndpoints;
using FastEndpoints.Swagger;
using IID.Infrastructure.Persistence;
using IID.Infrastructure.Realtime;
using Serilog;

namespace IID.Api.Extensions;

/// <summary>
/// Extension methods for database initialization and HTTP request pipeline configuration.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Runs pending EF Core migrations and data seeders at application startup.
    /// </summary>
    public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IidDbInitializer>();
        await initializer.InitializeAsync();
        return app;
    }

    /// <summary>
    /// Configures middleware pipeline, API endpoints, and SignalR hubs.
    /// </summary>
    public static WebApplication MapApi(this WebApplication app)
    {
        // Global exception handling
        app.UseExceptionHandler();

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
