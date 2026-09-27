using IID.Application.Common.Interfaces;
using IID.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace IID.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIidInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IClock, System.SystemClock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, Identity.HttpContextCurrentUser>();
        services.AddScoped<ITokenService, Services.TokenService>();
        services.AddScoped<IAuthService, Services.AuthService>();
        services.AddScoped<IUserDisplayNameProvider, Identity.UserDisplayNameProvider>();

        // Bounded in-memory cache for non-critical lookups (e.g., display-name lookup).
        services.AddMemoryCache(opt => opt.SizeLimit = 10_000);

        // Persistence Repositories & Unit of Work
        services.AddScoped<IUnitOfWork, Persistence.Repositories.UnitOfWork>();
        services.AddScoped<IDealershipRepository, Persistence.Repositories.DealershipRepository>();
        services.AddScoped<IVehicleRepository, Persistence.Repositories.VehicleRepository>();
        services.AddScoped<IVehicleActionRepository, Persistence.Repositories.VehicleActionRepository>();
        services.AddScoped<IUserActivityReadRepository, Persistence.Repositories.UserActivityReadRepository>();
        services.AddScoped<IID.Application.Features.Dashboard.Services.DashboardService>();
        services.AddScoped<IDashboardService, IID.Application.Features.Dashboard.Services.DashboardService>();

        // Domain events interceptor
        services.AddScoped<DomainEventDispatchInterceptor>();

        // Realtime contract mapper (Application DTO → Realtime wire DTO)
        services.AddScoped<IID.Infrastructure.Realtime.Contracts.IRealtimeContractMapper,
            IID.Infrastructure.Realtime.Contracts.RealtimeContractMapper>();

        return services;
    }
}
