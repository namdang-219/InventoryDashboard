using FastEndpoints;
using FastEndpoints.Swagger;
using IID.Api.Configurations;
using IID.Application.Common.Behaviors;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Dashboard.Services;
using IID.Infrastructure.Configuration;
using IID.Domain.Common;
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
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace IID.Api.Extensions;

/// <summary>
/// Composition-root helpers that mirror the Sportcast reference pattern:
/// every numbered registration block in <see cref="ConfigureServices"/> is implemented
/// as its own single-responsibility <c>Add…</c> extension method, mirroring Sportcast's
/// <c>AddBlobStorage</c>, <c>AddCosmosClientWithRetryPolicy</c>, <c>AddTranslationServices</c> etc.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Top-level composition root. Mirrors the Sportcast pattern: every registration
    /// (config, persistence, identity, signalR, app, swagger) is here, as a fluent chain
    /// of single-purpose helpers.
    /// </summary>
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1) Bind config → POCOs
        services.AddConfigurations(configuration);

        return services
            .AddHttpContextAccessors()
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
    /// Binds strongly-typed options sections (mirrors <c>AddConfigurations</c>).
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
    /// Section 2 — HTTP context accessor. Required by <c>HttpContextCurrentUser</c>.
    /// </summary>
    public static IServiceCollection AddHttpContextAccessors(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        return services;
    }

    /// <summary>
    /// Section 3 — Persistence. Wires the EF Core <see cref="IidDbContext"/> against SQL Server,
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
    /// Section 4 — ASP.NET Identity (Core) with roles, EF stores, and SignIn manager.
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
    /// Section 5a — JWT bearer authentication. Issuer / audience / signing key are bound
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
    /// Section 5b — Authorization policies.
    /// </summary>
    public static IServiceCollection AddIidAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Section 6 — SignalR + Application/Infrastructure composition: hubs, notifiers, repositories.
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
    /// Section 7 — CORS. Defaults are dev-friendly; configured origins come from <c>Cors:AllowedOrigins</c>.
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
    /// Section 8 — FastEndpoints + Swagger. Title / version come from the <c>Swagger</c> configuration section.
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

/// <summary>
/// Adapter from domain <see cref="ErrorKind"/> to HTTP status codes.
/// Endpoints call <c>ResultMapper.ToStatus</c> directly.
/// </summary>
public static class ResultMapper
{
    public static int ToStatus(ErrorKind k) => k switch
    {
        ErrorKind.ValidationFailed => 422,
        ErrorKind.NotFound => 404,
        ErrorKind.Conflict => 409,
        ErrorKind.Unauthorized => 401,
        ErrorKind.Forbidden => 403,
        ErrorKind.Internal => 500,
        _ => 500
    };
}
