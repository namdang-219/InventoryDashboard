using FastEndpoints;
using FastEndpoints.Swagger;
using IID.Api.Configurations;
using IID.Application.Common.Behaviors;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Dashboard.Services;
using IID.Infrastructure.Configuration;
using IID.Infrastructure;
using IID.Infrastructure.Identity;
using IID.Infrastructure.Persistence;
using IID.Infrastructure.Persistence.Interceptors;
using IID.Infrastructure.Persistence.Repositories;
using IID.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using IID.Api.Middleware;

namespace IID.Api.Extensions;

/// <summary>
/// Extension methods for configuring DI services and application dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all core application services and configurations.
    /// </summary>
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind config → POCOs
        services.AddConfigurations(configuration);

        return services
            .AddHttpContextAccessors()
            .AddIidExceptionHandling()
            .AddIidPersistence(configuration)
            .AddIidIdentity()
            .AddIidJwtAuthentication(configuration)
            .AddIidAuthorization()
            .AddIidRealtime()
            .AddIidApplication()
            .AddIidInfrastructure()
            .AddIidCors(configuration)
            .AddIidFastEndpointsWithSwagger(configuration);
    }

    /// <summary>
    /// Registers the global exception handler and RFC 7807 ProblemDetails support.
    /// </summary>
    public static IServiceCollection AddIidExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    /// <summary>
    /// Binds strongly-typed options sections from configuration.
    /// </summary>
    public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .Configure<JwtConfiguration>(configuration.GetSection(JwtConfiguration.Name))
            .Configure<SwaggerConfiguration>(configuration.GetSection(SwaggerConfiguration.Name))
            .Configure<CorsConfiguration>(configuration.GetSection(CorsConfiguration.Name));
        return services;
    }

    /// <summary>
    /// HTTP context accessor. Required by <c>HttpContextCurrentUser</c>.
    /// </summary>
    public static IServiceCollection AddHttpContextAccessors(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        return services;
    }

    /// <summary>
    /// Persistence. Wires the EF Core <see cref="IidDbContext"/> against SQL Server,
    /// and registers the database initializer (migrations + idempotent seeders) as a scoped service.
    /// </summary>
    public static IServiceCollection AddIidPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("IID")
            ?? throw new InvalidOperationException("ConnectionStrings:IID missing.");

        // Register the domain-event interceptor as a singleton so it can be
        // injected into IidDbContext's primary constructor.  It is safe to
        // be a singleton because it only holds an IServiceProvider reference.
        services.AddSingleton<DomainEventDispatchInterceptor>();

        services.AddDbContext<IidDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(connStr);
            opts.AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>());
            opts.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.Name));
        services.AddScoped<IidDbInitializer>();
        services.AddScoped<IidSeeder, IdentitySeeder>();
        services.AddScoped<IidSeeder, DealershipsSeeder>();
        services.AddScoped<IidSeeder, VehiclesSeeder>();
        services.AddScoped<IidSeeder, VehicleActionsSeeder>();
        return services;
    }

    /// <summary>
    /// ASP.NET Identity (Core) with roles, EF stores, and SignIn manager.
    /// </summary>
    public static IServiceCollection AddIidIdentity(this IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(o => o.SignIn.RequireConfirmedAccount = false)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IidDbContext>()
            .AddSignInManager();
        return services;
    }

    /// <summary>
    /// JWT bearer authentication. Issuer / audience / signing key are bound
    /// from the <c>Jwt</c> configuration section. Missing keys throw on startup, not at request time.
    /// </summary>
    public static IServiceCollection AddIidJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection(JwtConfiguration.Name).Get<JwtConfiguration>()
            ?? throw new InvalidOperationException("Jwt configuration section missing.");
        ArgumentException.ThrowIfNullOrWhiteSpace(jwtConfig.Key, "Jwt:Key");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key))
                };
            });
        return services;
    }

    /// <summary>
    /// Authorization policies.
    /// </summary>
    public static IServiceCollection AddIidAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// SignalR + Application/Infrastructure composition: hubs, notifiers, repositories.
    /// </summary>
    public static IServiceCollection AddIidRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<IVehicleHubNotifier>(sp =>
            new SignalRVehicleHubNotifier(
                sp.GetRequiredService<IHubContext<InventoryHub, IInventoryClient>>()));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDealershipRepository, DealershipRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IVehicleActionRepository, VehicleActionRepository>();
        services.AddScoped<IUserActivityReadRepository, UserActivityReadRepository>();
        services.AddScoped<DashboardService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }

    /// <summary>
    /// CORS. Defaults are dev-friendly; configured origins come from <c>Cors:AllowedOrigins</c>.
    /// </summary>
    public static IServiceCollection AddIidCors(this IServiceCollection services, IConfiguration configuration)
    {
        var cors = configuration.GetSection(CorsConfiguration.Name).Get<CorsConfiguration>()
            ?? new CorsConfiguration();
        services.AddCors(o =>
        {
            o.AddPolicy("default", b => b
                .WithOrigins(cors.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });
        return services;
    }

    /// <summary>
    /// FastEndpoints + Swagger. Title / version come from the <c>Swagger</c> configuration section.
    /// </summary>
    public static IServiceCollection AddIidFastEndpointsWithSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        var swagger = configuration.GetSection(SwaggerConfiguration.Name).Get<SwaggerConfiguration>()
            ?? new SwaggerConfiguration();
        services
            .AddFastEndpoints()
            .SwaggerDocument(o => o.DocumentSettings = s =>
            {
                s.Title = swagger.Title;
                s.Version = swagger.Version;
            });
        return services;
    }
}
